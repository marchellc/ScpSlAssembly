using System;
using System.Collections.Generic;
using DeathAnimations;
using Mirror;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerRoles.Ragdolls;

public class AutoHumanRagdoll : DynamicRagdoll
{
	[Serializable]
	private struct TrackedBone
	{
		public Transform OriginalTransform;

		public Rigidbody OriginalRigidbody;

		public List<Transform> Targets;
	}

	[SerializeField]
	[HideInInspector]
	private List<TrackedBone> _trackedBones;

	[SerializeField]
	private string _finderTempBoneName;

	[SerializeField]
	private Rigidbody _rootRigidbody;

	private bool _hasCuller;

	private CullableRig _culler;

	private readonly Dictionary<GameObject, AutoHumanRagdoll> _templates = new Dictionary<GameObject, AutoHumanRagdoll>();

	private static Transform _templateParent;

	private static Transform TemplateParent
	{
		get
		{
			if (AutoHumanRagdoll._templateParent == null)
			{
				GameObject obj = new GameObject("AutoHumanRagdoll Template Parent");
				obj.SetActive(value: false);
				UnityEngine.Object.DontDestroyOnLoad(obj);
				AutoHumanRagdoll._templateParent = obj.transform;
			}
			return AutoHumanRagdoll._templateParent;
		}
	}

	public bool PauseBoneMatching { get; set; }

	protected override void Start()
	{
		base.Start();
		if (base.TryGetComponent<CullableRig>(out this._culler))
		{
			this._hasCuller = true;
			this._culler.OnVisibleAgain += MatchBones;
		}
	}

	public override GameObject ClientHandleSpawn(SpawnMessage msg)
	{
		using NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(msg.payload);
		Compression.DecompressVarUInt(networkReaderPooled);
		networkReaderPooled.ReadByte();
		if (!PlayerRoleLoader.TryGetRoleTemplate<IFpcRole>((RoleTypeId)networkReaderPooled.ReadSByte(), out var result))
		{
			throw new InvalidOperationException("Serialization error in AutoHumanRagdoll. The component is not the first NetworkBehaviour in hierarchy or RoleId is not the first syncvar.");
		}
		return UnityEngine.Object.Instantiate(this.GetOrAddTemplate(result.FpcModule.CharacterModelTemplate).gameObject, msg.position, msg.rotation);
	}

	public override BasicRagdoll ServerInstantiateSelf(ReferenceHub owner, RoleTypeId targetRole)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<IFpcRole>(targetRole, out var result))
		{
			throw new InvalidOperationException("AutoHumanRagdoll is only available for FPC roles.");
		}
		GameObject characterModelTemplate = result.FpcModule.CharacterModelTemplate;
		AutoHumanRagdoll autoHumanRagdoll = UnityEngine.Object.Instantiate(this.GetOrAddTemplate(characterModelTemplate));
		SceneManager.MoveGameObjectToScene(autoHumanRagdoll.gameObject, SceneManager.GetActiveScene());
		return autoHumanRagdoll;
	}

	private AutoHumanRagdoll GetOrAddTemplate(GameObject sourceModel)
	{
		return this._templates.GetOrAdd(sourceModel, () => this.CreateNewTemplate(sourceModel));
	}

	private AutoHumanRagdoll CreateNewTemplate(GameObject sourceModel)
	{
		AutoHumanRagdoll autoHumanRagdoll = UnityEngine.Object.Instantiate(this, AutoHumanRagdoll.TemplateParent);
		autoHumanRagdoll.CreateNewFromSourceModel(sourceModel);
		return autoHumanRagdoll;
	}

	private void LateUpdate()
	{
		if (!base.Frozen && !this._rootRigidbody.IsSleeping() && (!this._hasCuller || !this._culler.IsCulled))
		{
			this.MatchBones();
		}
	}

	private void MatchBones()
	{
		if (this.PauseBoneMatching)
		{
			return;
		}
		foreach (TrackedBone trackedBone in this._trackedBones)
		{
			trackedBone.OriginalTransform.GetPositionAndRotation(out var position, out var rotation);
			foreach (Transform target in trackedBone.Targets)
			{
				target.SetPositionAndRotation(position, rotation);
			}
		}
	}

	private void CreateNewFromSourceModel(GameObject source)
	{
		this.FindTrackedBoneOriginals();
		GameObject obj = UnityEngine.Object.Instantiate(source);
		Transform transform = obj.transform;
		transform.SetParent(base.transform, worldPositionStays: false);
		transform.ResetLocalPose();
		Component[] componentsInChildren = obj.GetComponentsInChildren<Component>();
		foreach (Component component in componentsInChildren)
		{
			if (!(component is Transform) && !(component is Renderer) && !(component is LODGroup) && !(component is MeshFilter))
			{
				UnityEngine.Object.Destroy(component);
			}
		}
		this.PostprocessModel(transform);
		this.MatchBones();
	}

	private void FindTrackedBoneOriginals()
	{
		this._trackedBones.Clear();
		Rigidbody[] linkedRigidbodies = base.LinkedRigidbodies;
		foreach (Rigidbody rigidbody in linkedRigidbodies)
		{
			this._trackedBones.Add(new TrackedBone
			{
				OriginalRigidbody = rigidbody,
				OriginalTransform = rigidbody.transform,
				Targets = new List<Transform>()
			});
		}
	}

	private void PostprocessModel(Transform root)
	{
		base.AllDeathAnimations = base.GetComponentsInChildren<DeathAnimation>(includeInactive: true);
		if (base.TryGetComponent<CullableRig>(out var component))
		{
			component.SetTargetRenderers(new GameObject[1] { root.gameObject });
		}
		string text = base.name;
		base.name = this._finderTempBoneName;
		int layer = root.gameObject.layer;
		Transform[] componentsInChildren = root.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			transform.gameObject.layer = layer;
			foreach (TrackedBone trackedBone in this._trackedBones)
			{
				if (!(trackedBone.OriginalTransform.name != transform.name))
				{
					trackedBone.Targets.Add(transform);
				}
			}
		}
		base.name = text;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleLoader.OnLoaded = (Action)Delegate.Combine(PlayerRoleLoader.OnLoaded, new Action(OnRolesLoaded));
	}

	private static void OnRolesLoaded()
	{
		foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> allRole in PlayerRoleLoader.AllRoles)
		{
			if (allRole.Value is IRagdollRole { Ragdoll: AutoHumanRagdoll ragdoll } && allRole.Value is IFpcRole fpcRole)
			{
				GameObject characterModelTemplate = fpcRole.FpcModule.CharacterModelTemplate;
				ragdoll.GetOrAddTemplate(characterModelTemplate);
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}

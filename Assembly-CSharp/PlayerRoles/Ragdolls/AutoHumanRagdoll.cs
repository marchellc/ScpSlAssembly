using System;
using System.Collections.Generic;
using DeathAnimations;
using Mirror;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayerRoles.Ragdolls
{
	public class AutoHumanRagdoll : DynamicRagdoll
	{
		private static Transform TemplateParent
		{
			get
			{
				if (AutoHumanRagdoll._templateParent == null)
				{
					GameObject gameObject = new GameObject("AutoHumanRagdoll Template Parent");
					gameObject.SetActive(false);
					global::UnityEngine.Object.DontDestroyOnLoad(gameObject);
					AutoHumanRagdoll._templateParent = gameObject.transform;
				}
				return AutoHumanRagdoll._templateParent;
			}
		}

		protected override void Start()
		{
			base.Start();
			if (base.TryGetComponent<CullableRig>(out this._culler))
			{
				this._hasCuller = true;
				this._culler.OnVisibleAgain += this.MatchBones;
			}
		}

		public override GameObject ClientHandleSpawn(SpawnMessage msg)
		{
			GameObject gameObject;
			using (NetworkReaderPooled networkReaderPooled = NetworkReaderPool.Get(msg.payload))
			{
				Compression.DecompressVarUInt(networkReaderPooled);
				networkReaderPooled.ReadByte();
				IFpcRole fpcRole;
				if (!PlayerRoleLoader.TryGetRoleTemplate<IFpcRole>((RoleTypeId)networkReaderPooled.ReadSByte(), out fpcRole))
				{
					throw new InvalidOperationException("Serialization error in AutoHumanRagdoll. The component is not the first NetworkBehaviour in hierarchy or RoleId is not the first syncvar.");
				}
				gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this.GetOrAddTemplate(fpcRole.FpcModule.CharacterModelTemplate).gameObject, msg.position, msg.rotation);
			}
			return gameObject;
		}

		public override BasicRagdoll ServerInstantiateSelf(ReferenceHub owner, RoleTypeId targetRole)
		{
			IFpcRole fpcRole;
			if (!PlayerRoleLoader.TryGetRoleTemplate<IFpcRole>(targetRole, out fpcRole))
			{
				throw new InvalidOperationException("AutoHumanRagdoll is only available for FPC roles.");
			}
			GameObject characterModelTemplate = fpcRole.FpcModule.CharacterModelTemplate;
			AutoHumanRagdoll autoHumanRagdoll = global::UnityEngine.Object.Instantiate<AutoHumanRagdoll>(this.GetOrAddTemplate(characterModelTemplate));
			SceneManager.MoveGameObjectToScene(autoHumanRagdoll.gameObject, SceneManager.GetActiveScene());
			return autoHumanRagdoll;
		}

		private AutoHumanRagdoll GetOrAddTemplate(GameObject sourceModel)
		{
			return this._templates.GetOrAdd(sourceModel, () => this.CreateNewTemplate(sourceModel));
		}

		private AutoHumanRagdoll CreateNewTemplate(GameObject sourceModel)
		{
			AutoHumanRagdoll autoHumanRagdoll = global::UnityEngine.Object.Instantiate<AutoHumanRagdoll>(this, AutoHumanRagdoll.TemplateParent);
			autoHumanRagdoll.CreateNewFromSourceModel(sourceModel);
			return autoHumanRagdoll;
		}

		private void LateUpdate()
		{
			if (base.Frozen || this._rootRigidbody.IsSleeping())
			{
				return;
			}
			if (this._hasCuller && this._culler.IsCulled)
			{
				return;
			}
			this.MatchBones();
		}

		private void MatchBones()
		{
			foreach (AutoHumanRagdoll.TrackedBone trackedBone in this._trackedBones)
			{
				Vector3 vector;
				Quaternion quaternion;
				trackedBone.OriginalTransform.GetPositionAndRotation(out vector, out quaternion);
				foreach (Transform transform in trackedBone.Targets)
				{
					transform.SetPositionAndRotation(vector, quaternion);
				}
			}
		}

		private void CreateNewFromSourceModel(GameObject source)
		{
			this.FindTrackedBoneOriginals();
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(source);
			Transform transform = gameObject.transform;
			transform.SetParent(base.transform, false);
			transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			foreach (Component component in gameObject.GetComponentsInChildren<Component>())
			{
				if (!(component is Transform) && !(component is Renderer) && !(component is LODGroup) && !(component is MeshFilter))
				{
					global::UnityEngine.Object.Destroy(component);
				}
			}
			this.PostprocessModel(transform);
			this.MatchBones();
		}

		private void FindTrackedBoneOriginals()
		{
			this._trackedBones.Clear();
			foreach (Rigidbody rigidbody in this.LinkedRigidbodies)
			{
				this._trackedBones.Add(new AutoHumanRagdoll.TrackedBone
				{
					OriginalRigidbody = rigidbody,
					OriginalTransform = rigidbody.transform,
					Targets = new List<Transform>()
				});
			}
		}

		private void PostprocessModel(Transform root)
		{
			this.AllDeathAnimations = base.GetComponentsInChildren<DeathAnimation>(true);
			CullableRig cullableRig;
			if (base.TryGetComponent<CullableRig>(out cullableRig))
			{
				cullableRig.SetTargetRenderers(new GameObject[] { root.gameObject });
			}
			string name = base.name;
			base.name = this._finderTempBoneName;
			int layer = root.gameObject.layer;
			foreach (Transform transform in root.GetComponentsInChildren<Transform>())
			{
				transform.gameObject.layer = layer;
				foreach (AutoHumanRagdoll.TrackedBone trackedBone in this._trackedBones)
				{
					if (!(trackedBone.OriginalTransform.name != transform.name))
					{
						trackedBone.Targets.Add(transform);
					}
				}
			}
			base.name = name;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleLoader.OnLoaded = (Action)Delegate.Combine(PlayerRoleLoader.OnLoaded, new Action(AutoHumanRagdoll.OnRolesLoaded));
		}

		private static void OnRolesLoaded()
		{
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
			{
				IRagdollRole ragdollRole = keyValuePair.Value as IRagdollRole;
				if (ragdollRole != null)
				{
					AutoHumanRagdoll autoHumanRagdoll = ragdollRole.Ragdoll as AutoHumanRagdoll;
					if (autoHumanRagdoll != null)
					{
						IFpcRole fpcRole = keyValuePair.Value as IFpcRole;
						if (fpcRole != null)
						{
							GameObject characterModelTemplate = fpcRole.FpcModule.CharacterModelTemplate;
							autoHumanRagdoll.GetOrAddTemplate(characterModelTemplate);
						}
					}
				}
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		[SerializeField]
		[HideInInspector]
		private List<AutoHumanRagdoll.TrackedBone> _trackedBones;

		[SerializeField]
		private string _finderTempBoneName;

		[SerializeField]
		private Rigidbody _rootRigidbody;

		private bool _hasCuller;

		private CullableRig _culler;

		private readonly Dictionary<GameObject, AutoHumanRagdoll> _templates = new Dictionary<GameObject, AutoHumanRagdoll>();

		private static Transform _templateParent;

		[Serializable]
		private struct TrackedBone
		{
			public Transform OriginalTransform;

			public Rigidbody OriginalRigidbody;

			public List<Transform> Targets;
		}
	}
}

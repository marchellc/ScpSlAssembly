using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Ragdoll : DynamicRagdoll
{
	[SyncVar]
	private RoleTypeId _disguiseRole;

	[SerializeField]
	private float _revealDelay;

	[SerializeField]
	private float _revealDuration;

	[SerializeField]
	private Renderer _ownRenderer;

	private bool _playingAnimation;

	private float _revealElapsed;

	private Transform[] _trackedBones;

	private Material[] _humanMaterials;

	private Transform _humanRagdollRoot;

	private static readonly int ProgressHash = Shader.PropertyToID("_Progress");

	private static readonly int FadeHash = Shader.PropertyToID("_Fade");

	public RoleTypeId Network_disguiseRole
	{
		get
		{
			return this._disguiseRole;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._disguiseRole, 2uL, null);
		}
	}

	protected override void Start()
	{
		base.Start();
		if (base.Info.Handler is DisruptorDamageHandler || !PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this._disguiseRole, out var result) || !(result.Ragdoll.ServerInstantiateSelf(base.Info.OwnerHub, this._disguiseRole) is DynamicRagdoll dynamicRagdoll))
		{
			return;
		}
		this._ownRenderer.sharedMaterial = new Material(this._ownRenderer.sharedMaterial);
		this._trackedBones = dynamicRagdoll.LinkedRigidbodiesTransforms;
		this._playingAnimation = true;
		this._humanRagdollRoot = dynamicRagdoll.transform;
		this._humanRagdollRoot.ResetTransform();
		List<SkinnedMeshRenderer> list = ListPool<SkinnedMeshRenderer>.Shared.Rent();
		dynamicRagdoll.GetComponentsInChildren(includeInactive: true, list);
		this._humanMaterials = new Material[list.Count];
		for (int i = 0; i < this._humanMaterials.Length; i++)
		{
			SkinnedMeshRenderer skinnedMeshRenderer = list[i];
			Material material = (skinnedMeshRenderer.sharedMaterial = new Material(Scp3114FakeModelManager.GetVariant(skinnedMeshRenderer.sharedMaterial, Scp3114FakeModelManager.VariantType.Reveal)));
			material.SetFloat(Scp3114Ragdoll.ProgressHash, 0f);
			this._humanMaterials[i] = material;
			skinnedMeshRenderer.sharedMaterial = material;
		}
		ListPool<SkinnedMeshRenderer>.Shared.Return(list);
		dynamicRagdoll.FreezeRagdoll();
		dynamicRagdoll.gameObject.ForEachComponentInChildren(delegate(Component comp)
		{
			if (!(comp is Transform) && !(comp is Renderer))
			{
				Object.Destroy(comp);
			}
		}, includeInactive: true);
	}

	protected override void Update()
	{
		base.Update();
		if (!this._playingAnimation)
		{
			return;
		}
		for (int i = 0; i < this._trackedBones.Length; i++)
		{
			Transform transform = base.LinkedRigidbodiesTransforms[i];
			this._trackedBones[i].SetPositionAndRotation(transform.position, transform.rotation);
		}
		if (this._revealDelay > 0f)
		{
			this._revealDelay -= Time.deltaTime;
			return;
		}
		if (this._revealElapsed > this._revealDuration)
		{
			this._playingAnimation = false;
			Object.Destroy(this._humanRagdollRoot.gameObject);
			return;
		}
		this._revealElapsed += Time.deltaTime;
		float progress = Mathf.Clamp01(this._revealElapsed / this._revealDuration);
		this._ownRenderer.sharedMaterial.SetFloat(Scp3114Ragdoll.FadeHash, progress);
		this._humanMaterials.ForEach(delegate(Material x)
		{
			x.SetFloat(Scp3114Ragdoll.ProgressHash, progress);
		});
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		RagdollManager.ServerOnRagdollCreated += ServerOnRagdollCreated;
	}

	private static void ServerOnRagdollCreated(ReferenceHub owner, BasicRagdoll ragdoll)
	{
		if (ragdoll is Scp3114Ragdoll scp3114Ragdoll)
		{
			scp3114Ragdoll.Network_disguiseRole = RoleTypeId.None;
			if (!(owner == null) && owner.roleManager.CurrentRole is Scp3114Role { Disguised: not false } scp3114Role)
			{
				scp3114Ragdoll.Network_disguiseRole = scp3114Role.CurIdentity.StolenRole;
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteRoleType(this._disguiseRole);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteRoleType(this._disguiseRole);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._disguiseRole, null, reader.ReadRoleType());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._disguiseRole, null, reader.ReadRoleType());
		}
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.Ragdolls;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114Ragdoll : DynamicRagdoll
	{
		protected override void Start()
		{
			base.Start();
			if (this.Info.Handler is DisruptorDamageHandler)
			{
				return;
			}
			HumanRole humanRole;
			if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this._disguiseRole, out humanRole))
			{
				return;
			}
			DynamicRagdoll dynamicRagdoll = humanRole.Ragdoll.ServerInstantiateSelf(this.Info.OwnerHub, this._disguiseRole) as DynamicRagdoll;
			if (dynamicRagdoll == null)
			{
				return;
			}
			this._ownRenderer.sharedMaterial = new Material(this._ownRenderer.sharedMaterial);
			this._trackedBones = dynamicRagdoll.LinkedRigidbodiesTransforms;
			this._playingAnimation = true;
			this._humanRagdollRoot = dynamicRagdoll.transform;
			this._humanRagdollRoot.localScale = Vector3.one;
			this._humanRagdollRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
			List<SkinnedMeshRenderer> list = ListPool<SkinnedMeshRenderer>.Shared.Rent();
			dynamicRagdoll.GetComponentsInChildren<SkinnedMeshRenderer>(true, list);
			this._humanMaterials = new Material[list.Count];
			for (int i = 0; i < this._humanMaterials.Length; i++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = list[i];
				Material material = new Material(Scp3114FakeModelManager.GetVariant(skinnedMeshRenderer.sharedMaterial, Scp3114FakeModelManager.VariantType.Reveal));
				skinnedMeshRenderer.sharedMaterial = material;
				material.SetFloat(Scp3114Ragdoll.ProgressHash, 0f);
				this._humanMaterials[i] = material;
				skinnedMeshRenderer.sharedMaterial = material;
			}
			ListPool<SkinnedMeshRenderer>.Shared.Return(list);
			dynamicRagdoll.FreezeRagdoll();
			dynamicRagdoll.gameObject.ForEachComponentInChildren(delegate(Component comp)
			{
				if (comp is Transform || comp is Renderer)
				{
					return;
				}
				global::UnityEngine.Object.Destroy(comp);
			}, true);
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
				Transform transform = this.LinkedRigidbodiesTransforms[i];
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
				global::UnityEngine.Object.Destroy(this._humanRagdollRoot.gameObject);
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
			RagdollManager.ServerOnRagdollCreated += Scp3114Ragdoll.ServerOnRagdollCreated;
		}

		private static void ServerOnRagdollCreated(ReferenceHub owner, BasicRagdoll ragdoll)
		{
			Scp3114Ragdoll scp3114Ragdoll = ragdoll as Scp3114Ragdoll;
			if (scp3114Ragdoll == null)
			{
				return;
			}
			scp3114Ragdoll.Network_disguiseRole = RoleTypeId.None;
			if (owner == null)
			{
				return;
			}
			Scp3114Role scp3114Role = owner.roleManager.CurrentRole as Scp3114Role;
			if (scp3114Role == null)
			{
				return;
			}
			if (!scp3114Role.Disguised)
			{
				return;
			}
			scp3114Ragdoll.Network_disguiseRole = scp3114Role.CurIdentity.StolenRole;
		}

		public override bool Weaved()
		{
			return true;
		}

		public RoleTypeId Network_disguiseRole
		{
			get
			{
				return this._disguiseRole;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<RoleTypeId>(value, ref this._disguiseRole, 2UL, null);
			}
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
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteRoleType(this._disguiseRole);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<RoleTypeId>(ref this._disguiseRole, null, reader.ReadRoleType());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<RoleTypeId>(ref this._disguiseRole, null, reader.ReadRoleType());
			}
		}

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
	}
}

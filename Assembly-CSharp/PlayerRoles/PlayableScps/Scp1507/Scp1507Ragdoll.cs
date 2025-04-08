using System;
using System.Runtime.InteropServices;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp1507
{
	public class Scp1507Ragdoll : DynamicRagdoll
	{
		public float RevivalProgress
		{
			get
			{
				return this._revivalProgress;
			}
		}

		public bool ValidateRevive(ReferenceHub vocalizer)
		{
			if (this._hasAlreadyRevived || this.Info.RoleType.GetTeam() != vocalizer.GetTeam())
			{
				return false;
			}
			if (NetworkTime.time - this._lastResetTime > 12.0)
			{
				return false;
			}
			ReferenceHub ownerHub = this.Info.OwnerHub;
			if (!(ownerHub == null))
			{
				SpectatorRole spectatorRole = ownerHub.roleManager.CurrentRole as SpectatorRole;
				if (spectatorRole != null && spectatorRole.ReadyToRespawn)
				{
					return true;
				}
			}
			return false;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Scp1507VocalizeAbility.OnServerVocalize -= this.OnVocalize;
		}

		protected override void Start()
		{
			this.SpawnVariant(this.Info.RoleType);
			this.Network_lastResetTime = NetworkTime.time;
			base.Start();
			Scp1507VocalizeAbility.OnServerVocalize += this.OnVocalize;
		}

		private void FixedUpdate()
		{
			if (this._unfreezeDelay <= 0)
			{
				return;
			}
			this._unfreezeDelay--;
			if (this._unfreezeDelay > 0)
			{
				return;
			}
			foreach (Rigidbody rigidbody in this.LinkedRigidbodies)
			{
				if (!(rigidbody == null))
				{
					rigidbody.isKinematic = false;
				}
			}
		}

		private void SpawnVariant(RoleTypeId role)
		{
			GameObject[] array;
			switch (role)
			{
			case RoleTypeId.Flamingo:
				array = this._regularVariants;
				break;
			case RoleTypeId.AlphaFlamingo:
				array = this._alphaVariants;
				break;
			case RoleTypeId.ZombieFlamingo:
				array = this._zombieVariants;
				break;
			default:
				return;
			}
			GameObject gameObject = array.RandomItem<GameObject>();
			GameObject gameObject2 = global::UnityEngine.Object.Instantiate<GameObject>(gameObject, base.transform);
			gameObject2.transform.SetLocalPositionAndRotation(gameObject.transform.localPosition, gameObject.transform.localRotation);
			this.LinkedRigidbodies = gameObject2.GetComponentsInChildren<Rigidbody>();
			this.LinkedRigidbodiesTransforms = new Transform[this.LinkedRigidbodies.Length];
			for (int i = 0; i < this.LinkedRigidbodies.Length; i++)
			{
				Rigidbody rigidbody = this.LinkedRigidbodies[i];
				rigidbody.isKinematic = true;
				this.LinkedRigidbodiesTransforms[i] = rigidbody.transform;
			}
			this._unfreezeDelay = 3;
		}

		private void OnVocalize(ReferenceHub hub)
		{
			if (!NetworkServer.active || !this.ValidateRevive(hub))
			{
				return;
			}
			Scp1507Role scp1507Role = hub.roleManager.CurrentRole as Scp1507Role;
			bool flag = false;
			for (int i = 0; i < this.LinkedRigidbodiesTransforms.Length; i++)
			{
				if (scp1507Role.SqrDistanceTo(this.LinkedRigidbodiesTransforms[i].position) <= 15f)
				{
					flag = true;
					break;
				}
			}
			if (!flag && scp1507Role.SqrDistanceTo(this.Info.StartPosition) > 15f)
			{
				return;
			}
			float num = 0.083333336f;
			if (scp1507Role.RoleTypeId == RoleTypeId.AlphaFlamingo)
			{
				num *= 2f;
			}
			this.Network_lastResetTime = NetworkTime.time;
			this.Network_revivalProgress = this._revivalProgress + num;
			if (this._revivalProgress < 1f)
			{
				return;
			}
			this.Info.OwnerHub.roleManager.ServerSetRole(this.Info.RoleType, RoleChangeReason.Revived, RoleSpawnFlags.All);
			NetworkServer.Destroy(base.gameObject);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			RagdollManager.ServerOnRagdollCreated += Scp1507Ragdoll.ServerOnRagdollCreated;
		}

		private static void ServerOnRagdollCreated(ReferenceHub owner, BasicRagdoll basicRagdoll)
		{
			Scp1507Role scp1507Role = owner.roleManager.CurrentRole as Scp1507Role;
			if (scp1507Role == null)
			{
				return;
			}
			Scp1507Ragdoll scp1507Ragdoll = basicRagdoll as Scp1507Ragdoll;
			if (scp1507Ragdoll == null)
			{
				return;
			}
			scp1507Ragdoll.Network_hasAlreadyRevived = scp1507Role.AlreadyRevived;
		}

		public override bool Weaved()
		{
			return true;
		}

		public float Network_revivalProgress
		{
			get
			{
				return this._revivalProgress;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<float>(value, ref this._revivalProgress, 2UL, null);
			}
		}

		public bool Network_hasAlreadyRevived
		{
			get
			{
				return this._hasAlreadyRevived;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this._hasAlreadyRevived, 4UL, null);
			}
		}

		public double Network_lastResetTime
		{
			get
			{
				return this._lastResetTime;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<double>(value, ref this._lastResetTime, 8UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteFloat(this._revivalProgress);
				writer.WriteBool(this._hasAlreadyRevived);
				writer.WriteDouble(this._lastResetTime);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteFloat(this._revivalProgress);
			}
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteBool(this._hasAlreadyRevived);
			}
			if ((base.syncVarDirtyBits & 8UL) != 0UL)
			{
				writer.WriteDouble(this._lastResetTime);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._revivalProgress, null, reader.ReadFloat());
				base.GeneratedSyncVarDeserialize<bool>(ref this._hasAlreadyRevived, null, reader.ReadBool());
				base.GeneratedSyncVarDeserialize<double>(ref this._lastResetTime, null, reader.ReadDouble());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<float>(ref this._revivalProgress, null, reader.ReadFloat());
			}
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._hasAlreadyRevived, null, reader.ReadBool());
			}
			if ((num & 8L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<double>(ref this._lastResetTime, null, reader.ReadDouble());
			}
		}

		public const float RevivalMaxTime = 12f;

		private const float ReviveDistanceSqr = 15f;

		private const float ReviveProgressAddition = 0.083333336f;

		private const float ReviveAlphaBoost = 2f;

		private const int FreezeFixedFrames = 3;

		[SyncVar]
		private float _revivalProgress;

		[SyncVar]
		private bool _hasAlreadyRevived;

		[SyncVar]
		private double _lastResetTime;

		[SerializeField]
		private GameObject[] _alphaVariants;

		[SerializeField]
		private GameObject[] _regularVariants;

		[SerializeField]
		private GameObject[] _zombieVariants;

		private int _unfreezeDelay;
	}
}

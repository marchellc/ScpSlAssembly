using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using Footprinting;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;

namespace Hazards
{
	public class TantrumEnvironmentalHazard : TemporaryHazard
	{
		public override Vector3 SourcePosition
		{
			get
			{
				return this._correctPosition.position + this.SourceOffset;
			}
			set
			{
				base.transform.position = value;
			}
		}

		public RelativePosition SynchronizedPosition
		{
			[CompilerGenerated]
			get
			{
				return this.<SynchronizedPosition>k__BackingField;
			}
			[CompilerGenerated]
			set
			{
				this.Network<SynchronizedPosition>k__BackingField = value;
			}
		}

		public bool PlaySizzle { get; set; }

		public override float HazardDuration { get; set; } = 180f;

		public override float DecaySpeed
		{
			get
			{
				if (this._decaySpeedOverride >= 0f)
				{
					return this._decaySpeedOverride;
				}
				float num = 1f;
				foreach (Scp244DeployablePickup scp244DeployablePickup in Scp244DeployablePickup.Instances)
				{
					num += scp244DeployablePickup.FogPercentForPoint(this.SourcePosition);
				}
				return num;
			}
		}

		public override bool OnEnter(ReferenceHub player)
		{
			if (!this.IsActive || player.IsSCP(true))
			{
				return false;
			}
			if (!base.OnEnter(player))
			{
				return false;
			}
			player.playerEffectsController.EnableEffect<Stained>(1f, false);
			PlayerEvents.OnEnteredHazard(new PlayerEnteredHazardEventArgs(player, this));
			return true;
		}

		public override void OnStay(ReferenceHub player)
		{
			player.playerEffectsController.EnableEffect<Stained>(1f, false);
		}

		public override bool OnExit(ReferenceHub player)
		{
			if (!base.OnExit(player))
			{
				return false;
			}
			if (!this.IsActive || player.IsSCP(true))
			{
				return false;
			}
			player.playerEffectsController.EnableEffect<Stained>(2f, false);
			PlayerEvents.OnLeftHazard(new PlayerLeftHazardEventArgs(player, this));
			return true;
		}

		protected override void Start()
		{
			base.Start();
			this.ClientApplyDecalSize();
			if (!NetworkServer.active)
			{
				return;
			}
			TantrumEnvironmentalHazard.AllTantrums.Add(this);
			ExplosionGrenade.OnExploded += this.CheckExplosion;
		}

		protected override void ClientApplyDecalSize()
		{
		}

		[Server]
		public override void ServerDestroy()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Hazards.TantrumEnvironmentalHazard::ServerDestroy()' called when server was not active");
				return;
			}
			base.ServerDestroy();
			this.RpcDespawn(this.PlaySizzle);
			this.ServerDelayedDestroy(6f);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (!NetworkServer.active)
			{
				return;
			}
			TantrumEnvironmentalHazard.AllTantrums.Remove(this);
			ExplosionGrenade.OnExploded -= this.CheckExplosion;
		}

		[ClientRpc]
		private void RpcDespawn(bool playSizzle)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteBool(playSizzle);
			this.SendRPCInternal("System.Void Hazards.TantrumEnvironmentalHazard::RpcDespawn(System.Boolean)", -963326249, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void ServerDelayedDestroy(float waitTime)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Timing.RunCoroutine(this.DelayedPuddleRemoval(waitTime).CancelWith(base.gameObject));
		}

		private void LateUpdate()
		{
			this.SourcePosition = this.SynchronizedPosition.Position;
		}

		private void CheckExplosion(Footprint attacker, Vector3 pos, ExplosionGrenade grenade)
		{
			Vector3 position = this._correctPosition.position;
			if (Mathf.Abs(pos.y - position.y) > 5f)
			{
				return;
			}
			float num = this._explodeDistance * this._explodeDistance;
			if ((position - pos).SqrMagnitudeIgnoreY() > num)
			{
				return;
			}
			this.PlaySizzle = true;
			this.ServerDestroy();
		}

		private IEnumerator<float> DelayedPuddleRemoval(float waitTime)
		{
			yield return Timing.WaitForSeconds(waitTime);
			NetworkServer.Destroy(base.gameObject);
			yield break;
		}

		static TantrumEnvironmentalHazard()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(TantrumEnvironmentalHazard), "System.Void Hazards.TantrumEnvironmentalHazard::RpcDespawn(System.Boolean)", new RemoteCallDelegate(TantrumEnvironmentalHazard.InvokeUserCode_RpcDespawn__Boolean));
		}

		public override bool Weaved()
		{
			return true;
		}

		public RelativePosition Network<SynchronizedPosition>k__BackingField
		{
			get
			{
				return this.<SynchronizedPosition>k__BackingField;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<RelativePosition>(value, ref this.<SynchronizedPosition>k__BackingField, 1UL, null);
			}
		}

		protected void UserCode_RpcDespawn__Boolean(bool playSizzle)
		{
		}

		protected static void InvokeUserCode_RpcDespawn__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcDespawn called on server.");
				return;
			}
			((TantrumEnvironmentalHazard)obj).UserCode_RpcDespawn__Boolean(reader.ReadBool());
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteRelativePosition(this.<SynchronizedPosition>k__BackingField);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteRelativePosition(this.<SynchronizedPosition>k__BackingField);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<RelativePosition>(ref this.<SynchronizedPosition>k__BackingField, null, reader.ReadRelativePosition());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<RelativePosition>(ref this.<SynchronizedPosition>k__BackingField, null, reader.ReadRelativePosition());
			}
		}

		public static readonly List<TantrumEnvironmentalHazard> AllTantrums = new List<TantrumEnvironmentalHazard>();

		private const float ExplosionHeight = 5f;

		private const float DelayedDestroy = 6f;

		private float _decaySpeedOverride = -1f;

		private readonly float _explodeDistance = 5.25f;

		[SerializeField]
		private Transform _correctPosition;
	}
}

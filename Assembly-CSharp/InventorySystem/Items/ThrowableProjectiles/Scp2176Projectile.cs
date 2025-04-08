using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AudioPooling;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class Scp2176Projectile : EffectGrenade
	{
		public static event Action<Scp2176Projectile, RoomIdentifier> OnServerShattered;

		protected override void ProcessCollision(Collision collision)
		{
			float sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
			if (NetworkServer.active && sqrMagnitude >= 42.25f)
			{
				base.ProcessCollision(collision);
				if (this._hasTriggered)
				{
					return;
				}
				this.ServerActivate();
				if (sqrMagnitude >= 72.25f)
				{
					this.ServerFuseEnd();
					return;
				}
				if (!this._playedDropSound)
				{
					this.Network_playedDropSound = true;
					this.RpcMakeSound();
				}
			}
		}

		public override bool ServerFuseEnd()
		{
			if (!base.ServerFuseEnd())
			{
				return false;
			}
			this._hasTriggered = true;
			this.ServerShatter();
			ServerEvents.OnProjectileExploded(new ProjectileExplodedEventArgs(this, this.PreviousOwner.Hub, base.transform.position));
			return true;
		}

		[ClientRpc]
		public void RpcMakeSound()
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void InventorySystem.Items.ThrowableProjectiles.Scp2176Projectile::RpcMakeSound()", 1009999061, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public override void ServerActivate()
		{
			base.ServerActivate();
			PickupSyncInfo info = this.Info;
			info.Locked = true;
			base.NetworkInfo = info;
		}

		public void ServerImmediatelyShatter()
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Tried to call ServerImmediatelyShatter from the client!");
			}
			this.ServerActivate();
			this.ServerFuseEnd();
		}

		private void ServerShatter()
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Tried to call ServerShatter from the client!");
			}
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(base.transform.position, true);
			if (roomIdentifier == null)
			{
				return;
			}
			Action<Scp2176Projectile, RoomIdentifier> onServerShattered = Scp2176Projectile.OnServerShattered;
			if (onServerShattered != null)
			{
				onServerShattered(this, roomIdentifier);
			}
			TeslaGate teslaGate;
			if (roomIdentifier.Name == RoomName.HczTesla && Scp2176Projectile.TryFindTeslaAtRoom(roomIdentifier, out teslaGate))
			{
				this.ServerOverloadTesla(roomIdentifier, teslaGate, roomIdentifier.LightControllers);
			}
			else
			{
				foreach (RoomLightController roomLightController in roomIdentifier.LightControllers)
				{
					roomLightController.ServerFlickerLights(roomLightController.LightsEnabled ? this.LockdownDuration : 0.1f);
				}
			}
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(roomIdentifier, out hashSet))
			{
				return;
			}
			this.ServerLockdown(hashSet);
		}

		private static bool TryFindTeslaAtRoom(RoomIdentifier rid, out TeslaGate gate)
		{
			return TeslaGate.AllGates.TryGetFirst((TeslaGate x) => rid == RoomUtils.RoomAtPosition(x.transform.position), out gate);
		}

		private void ServerOverloadTesla(RoomIdentifier rid, TeslaGate tg, IEnumerable<RoomLightController> lightControllers)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Tried to call ServerOverloadTesla from the client!");
			}
			foreach (RoomLightController roomLightController in lightControllers)
			{
				roomLightController.ServerFlickerLights(roomLightController.LightsEnabled ? (this.LockdownDuration - 1f) : 0.1f);
			}
			tg.NetworkInactiveTime = ((tg.InactiveTime > 0f) ? 0.1f : (this.LockdownDuration - 1f));
			tg.RpcInstantBurst();
			tg.ServerSideIdle(false);
		}

		private void ServerLockdown(IEnumerable<DoorVariant> doors)
		{
			bool inProgress = AlphaWarheadController.InProgress;
			foreach (DoorVariant doorVariant in doors)
			{
				INonInteractableDoor nonInteractableDoor = doorVariant as INonInteractableDoor;
				if (nonInteractableDoor == null || !nonInteractableDoor.IgnoreLockdowns)
				{
					DoorLockReason activeLocks = (DoorLockReason)doorVariant.ActiveLocks;
					if (!doorVariant.TargetState && (activeLocks.HasFlagFast(DoorLockReason.Lockdown079) || activeLocks.HasFlagFast(DoorLockReason.Lockdown2176) || activeLocks.HasFlagFast(DoorLockReason.Regular079)))
					{
						doorVariant.UnlockLater(0f, DoorLockReason.Lockdown2176);
						if (!doorVariant.RequiredPermissions.Bypass2176)
						{
							doorVariant.NetworkTargetState = true;
						}
					}
					else
					{
						DoorLockMode mode = DoorLockUtils.GetMode((DoorLockReason)doorVariant.ActiveLocks);
						if (mode.HasFlagFast(DoorLockMode.CanClose) || mode.HasFlagFast(DoorLockMode.ScpOverride))
						{
							doorVariant.ServerChangeLock(DoorLockReason.Lockdown2176, true);
							doorVariant.UnlockLater(this.LockdownDuration, DoorLockReason.Lockdown2176);
							if (!inProgress)
							{
								doorVariant.NetworkTargetState = false;
							}
						}
					}
				}
			}
		}

		public override bool Weaved()
		{
			return true;
		}

		public bool Network_playedDropSound
		{
			get
			{
				return this._playedDropSound;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<bool>(value, ref this._playedDropSound, 4UL, null);
			}
		}

		protected void UserCode_RpcMakeSound()
		{
			AudioSourcePoolManager.PlayOnTransform(this._dropSound, base.gameObject.transform, 20f, 0.5f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		protected static void InvokeUserCode_RpcMakeSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcMakeSound called on server.");
				return;
			}
			((Scp2176Projectile)obj).UserCode_RpcMakeSound();
		}

		static Scp2176Projectile()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2176Projectile), "System.Void InventorySystem.Items.ThrowableProjectiles.Scp2176Projectile::RpcMakeSound()", new RemoteCallDelegate(Scp2176Projectile.InvokeUserCode_RpcMakeSound));
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteBool(this._playedDropSound);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 4UL) != 0UL)
			{
				writer.WriteBool(this._playedDropSound);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._playedDropSound, null, reader.ReadBool());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 4L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<bool>(ref this._playedDropSound, null, reader.ReadBool());
			}
		}

		public float LockdownDuration = 13f;

		private const float LockdownDisableValue = 0.1f;

		private const float PanicDuration = 5f;

		private const float ShatterVelocity = 8.5f;

		private const float ActivateVelocity = 6.5f;

		private const float DropSoundRange = 20f;

		private bool _hasTriggered;

		[SerializeField]
		private AudioClip _dropSound;

		[SyncVar]
		private bool _playedDropSound;
	}
}

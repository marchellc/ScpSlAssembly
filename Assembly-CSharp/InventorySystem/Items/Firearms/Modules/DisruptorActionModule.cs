using System;
using System.Collections.Generic;
using Footprinting;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Extensions;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DisruptorActionModule : ModuleBase, IReloaderModule, IActionModule, IBusyIndicatorModule
	{
		public DisruptorActionModule.FiringState CurFiringState
		{
			get
			{
				return this._curFiringState;
			}
			private set
			{
				this._curFiringState = value;
				if (value != DisruptorActionModule.FiringState.None)
				{
					this._lastActiveFiringState = value;
					return;
				}
				this._firingElapsed = 0f;
			}
		}

		public bool IsFiring
		{
			get
			{
				return this.CurFiringState > DisruptorActionModule.FiringState.None;
			}
		}

		public bool IsReloading
		{
			get
			{
				return this.GetDisplayReloadingOrUnloading(base.ItemSerial);
			}
		}

		public bool IsUnloading
		{
			get
			{
				return false;
			}
		}

		public bool IsLoaded
		{
			get
			{
				return this._magModule.AmmoStored > 0 || this.CurFiringState > DisruptorActionModule.FiringState.None;
			}
		}

		public bool IsBusy
		{
			get
			{
				return this.IsFiring || this.IsReloading;
			}
		}

		public float DisplayCyclicRate { get; private set; }

		[ExposedFirearmEvent]
		public void EndShootingAnimation()
		{
			this.CurFiringState = DisruptorActionModule.FiringState.None;
			DisruptorActionModule.ReloadRequiredSerials.Add(base.ItemSerial);
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload, false);
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(DisruptorActionModule.MessageType.RpcRequireReloadTrue);
				}, true);
			}
		}

		[ExposedFirearmEvent]
		public void FinishReloading()
		{
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(DisruptorActionModule.MessageType.RpcRequireReloadFalse);
				}, true);
			}
		}

		public bool GetDisplayReloadingOrUnloading(ushort serial)
		{
			return DisruptorActionModule.ReloadRequiredSerials.Contains(serial);
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (reader.ReadByte())
			{
			case 0:
				DisruptorActionModule.ReloadRequiredSerials.Add(serial);
				return;
			case 1:
				DisruptorActionModule.ReloadRequiredSerials.Remove(serial);
				return;
			case 2:
				DisruptorActionModule.ReloadRequiredSerials.Clear();
				while (reader.Remaining > 0)
				{
					DisruptorActionModule.ReloadRequiredSerials.Add(reader.ReadUShort());
				}
				return;
			case 3:
				break;
			case 4:
			{
				DisruptorActionModule.FiringState firingState = (DisruptorActionModule.FiringState)reader.ReadByte();
				ShotEventManager.Trigger(new DisruptorShotEvent(new ItemIdentifier(base.Firearm.ItemTypeId, serial), default(Footprint), firingState));
				break;
			}
			default:
				return;
			}
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			if (reader.ReadByte() == 3 && (!base.IsLocalPlayer || base.IsServer))
			{
				bool flag = reader.ReadBool();
				bool flag2 = reader.ReadBool();
				this.StartFiring(flag, flag2);
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			DisruptorActionModule.MessageType messageType = (DisruptorActionModule.MessageType)reader.ReadByte();
			if (messageType == DisruptorActionModule.MessageType.CmdRequestStartFiring)
			{
				this.ServerProcessStartCmd(reader.ReadBool());
				return;
			}
			if (messageType != DisruptorActionModule.MessageType.CmdConfirmDischarge)
			{
				return;
			}
			this._receivedShots.Enqueue(new ShotBacktrackData(reader));
			this.ServerUpdateShotRequests();
		}

		protected override void OnInit()
		{
			base.OnInit();
			if (!base.Firearm.TryGetModules(out this._magModule, out this._modeSelector, out this._audioModule))
			{
				throw new NotImplementedException("The DisruptorActionModule does not implement all modules necessary for its operation.");
			}
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this.UpdateAction();
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, this.IsReloading, false);
			if (base.IsLocalPlayer)
			{
				this.ClientUpdate();
			}
			if (base.IsServer)
			{
				this.ServerUpdateShotRequests();
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			DisruptorActionModule.ReloadRequiredSerials.Clear();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
		{
			base.ServerOnPlayerConnected(hub, firstModule);
			if (!firstModule)
			{
				return;
			}
			this.SendRpc(hub, delegate(NetworkWriter x)
			{
				x.WriteSubheader(DisruptorActionModule.MessageType.RpcRequireReloadFullResync);
				DisruptorActionModule.ReloadRequiredSerials.ForEach(new Action<ushort>(x.WriteUShort));
			});
		}

		internal override void OnRemoved(ItemPickupBase pickupBase)
		{
			base.OnRemoved(pickupBase);
			FirearmPickup firearmPickup = pickupBase as FirearmPickup;
			if (firearmPickup == null)
			{
				return;
			}
			float[] array;
			if (pickupBase == null || !this.TryGetCurStateTimes(out array))
			{
				return;
			}
			DisruptorWorldmodelActionExtension disruptorWorldmodelActionExtension;
			if (!firearmPickup.Worldmodel.TryGetExtension<DisruptorWorldmodelActionExtension>(out disruptorWorldmodelActionExtension))
			{
				return;
			}
			float[] array2 = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i] - this._firingElapsed;
			}
			disruptorWorldmodelActionExtension.ServerScheduleShot(new Footprint(base.Firearm.Owner), this.CurFiringState, array2);
			if (base.IsServer)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(DisruptorActionModule.MessageType.RpcRequireReloadTrue);
				}, true);
			}
		}

		private void UpdateAction()
		{
			float[] array;
			if (!this.TryGetCurStateTimes(out array))
			{
				return;
			}
			float num = this._firingElapsed + Time.deltaTime;
			foreach (float num2 in array)
			{
				if (num2 > this._firingElapsed && num2 <= num)
				{
					this.TriggerFire();
				}
			}
			this._firingElapsed = num;
		}

		private bool TryGetCurStateTimes(out float[] times)
		{
			DisruptorActionModule.FiringState curFiringState = this.CurFiringState;
			if (curFiringState == DisruptorActionModule.FiringState.FiringRapid)
			{
				times = this._rapidShotTimes;
				return true;
			}
			if (curFiringState != DisruptorActionModule.FiringState.FiringSingle)
			{
				times = null;
				return false;
			}
			times = this._singleShotTimes;
			return true;
		}

		private void TriggerFire()
		{
			if (base.IsServer)
			{
				this._shotTickets.Enqueue(NetworkTime.time);
				this.ServerUpdateShotRequests();
			}
			if (base.IsLocalPlayer)
			{
				ShotEventManager.Trigger(new DisruptorShotEvent(base.Firearm, this.CurFiringState));
				this.SendCmd(delegate(NetworkWriter writer)
				{
					writer.WriteSubheader(DisruptorActionModule.MessageType.CmdConfirmDischarge);
					writer.WriteBacktrackData(new ShotBacktrackData(base.Firearm));
				});
			}
		}

		private void ServerUpdateShotRequests()
		{
			double num;
			if (!this._shotTickets.TryPeek(out num))
			{
				return;
			}
			ShotBacktrackData shotBacktrackData;
			if (this._receivedShots.TryDequeue(out shotBacktrackData))
			{
				shotBacktrackData.ProcessShot(base.Firearm, new Action<ReferenceHub>(this.ServerFire));
			}
			else
			{
				if (NetworkTime.time - num < 0.07000000029802322)
				{
					return;
				}
				this.ServerFire(null);
			}
			this._shotTickets.Dequeue();
		}

		private void ServerProcessStartCmd(bool ads)
		{
			if (this.IsReloading || this.CurFiringState != DisruptorActionModule.FiringState.None || this._magModule.AmmoStored == 0)
			{
				return;
			}
			PlayerShootingWeaponEventArgs playerShootingWeaponEventArgs = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnShootingWeapon(playerShootingWeaponEventArgs);
			if (!playerShootingWeaponEventArgs.IsAllowed)
			{
				return;
			}
			bool last = this._magModule.AmmoStored == 1;
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(DisruptorActionModule.MessageType.RpcStartFiring);
				x.WriteBool(ads);
				x.WriteBool(last);
			}, true);
			this._magModule.ServerModifyAmmo(-1);
			PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
		}

		private void ServerFire(ReferenceHub primaryTarget)
		{
			IHitregModule hitregModule;
			if (base.Firearm.TryGetModule(out hitregModule, true))
			{
				hitregModule.Fire(primaryTarget, new DisruptorShotEvent(base.Firearm, this._lastActiveFiringState));
			}
			this.SendRpc((ReferenceHub hub) => hub != base.Firearm.Owner, delegate(NetworkWriter x)
			{
				x.WriteSubheader(DisruptorActionModule.MessageType.RpcOnShot);
				x.WriteByte((byte)this._lastActiveFiringState);
			});
		}

		private void ClientUpdate()
		{
			if (base.Firearm.AnyModuleBusy(null))
			{
				return;
			}
			if (!base.GetAction(ActionName.Shoot))
			{
				return;
			}
			if (base.PrimaryActionBlocked)
			{
				return;
			}
			if (this.IsReloading)
			{
				return;
			}
			if (this.CurFiringState != DisruptorActionModule.FiringState.None)
			{
				return;
			}
			if (!base.IsServer)
			{
				this.StartFiring(this._modeSelector.SingleShotSelected, this._magModule.AmmoStored <= 1);
			}
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(DisruptorActionModule.MessageType.CmdRequestStartFiring);
				x.WriteBool(this._modeSelector.SingleShotSelected);
			});
		}

		private void StartFiring(bool singleMode, bool last)
		{
			if (singleMode)
			{
				this.CurFiringState = DisruptorActionModule.FiringState.FiringSingle;
				base.Firearm.AnimSetTrigger(last ? DisruptorActionModule.FireSingleLastHash : DisruptorActionModule.FireSingleNormalHash, false);
			}
			else
			{
				this.CurFiringState = DisruptorActionModule.FiringState.FiringRapid;
				base.Firearm.AnimSetTrigger(last ? DisruptorActionModule.FireRapidLastHash : DisruptorActionModule.FireRapidNormalHash, false);
			}
			this._audioModule.PlayDisruptorShot(singleMode, last);
		}

		private const float ShotTicketTimeout = 0.07f;

		private static readonly int FireSingleNormalHash = Animator.StringToHash("FireSingle");

		private static readonly int FireSingleLastHash = Animator.StringToHash("FireSingleLast");

		private static readonly int FireRapidNormalHash = Animator.StringToHash("FireRapid");

		private static readonly int FireRapidLastHash = Animator.StringToHash("FireRapidLast");

		private static readonly HashSet<ushort> ReloadRequiredSerials = new HashSet<ushort>();

		private readonly FullAutoShotsQueue<ShotBacktrackData> _receivedShots = new FullAutoShotsQueue<ShotBacktrackData>((ShotBacktrackData x) => x);

		private readonly Queue<double> _shotTickets = new Queue<double>();

		private MagazineModule _magModule;

		private DisruptorModeSelector _modeSelector;

		private DisruptorAudioModule _audioModule;

		private DisruptorActionModule.FiringState _curFiringState;

		private DisruptorActionModule.FiringState _lastActiveFiringState;

		private float _firingElapsed;

		[SerializeField]
		private float[] _singleShotTimes;

		[SerializeField]
		private float[] _rapidShotTimes;

		private enum MessageType
		{
			RpcRequireReloadTrue,
			RpcRequireReloadFalse,
			RpcRequireReloadFullResync,
			RpcStartFiring,
			RpcOnShot,
			CmdRequestStartFiring,
			CmdConfirmDischarge
		}

		public enum FiringState
		{
			None,
			FiringRapid,
			FiringSingle
		}
	}
}

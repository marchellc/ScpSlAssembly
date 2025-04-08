using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;
using Utils;
using Utils.NonAllocLINQ;

namespace InventorySystem.Items.Firearms.Modules
{
	public class DoubleActionModule : ModuleBase, IActionModule, IBusyIndicatorModule, IInspectPreventerModule
	{
		public static event Action<ushort, bool> OnCockedChanged;

		public bool IsBusy
		{
			get
			{
				return this.OnCooldown || this.IsCocking || this._triggerPull.IsPulling;
			}
		}

		public bool InspectionAllowed
		{
			get
			{
				if (this._cylinderModule.AmmoStored != 0)
				{
					return !this.IsBusy;
				}
				return !this.IsCocking;
			}
		}

		public bool Cocked
		{
			get
			{
				if (!base.IsServer)
				{
					return this._clientCocked.Value;
				}
				return DoubleActionModule.GetCocked(base.ItemSerial);
			}
			private set
			{
				if (!base.IsServer)
				{
					this._clientCocked.Value = value;
					return;
				}
				if (value)
				{
					DoubleActionModule.CockedHashes.Add(base.ItemSerial);
					this.SendRpc(DoubleActionModule.MessageType.RpcSetCockedTrue, null, true);
					return;
				}
				DoubleActionModule.CockedHashes.Remove(base.ItemSerial);
				this.SendRpc(DoubleActionModule.MessageType.RpcSetCockedFalse, null, true);
			}
		}

		public float TriggerPullProgress
		{
			get
			{
				return this._triggerPull.PullProgress;
			}
		}

		public float DisplayCyclicRate
		{
			get
			{
				return 1f / (this.DoubleActionTime + this.TimeBetweenShots);
			}
		}

		public bool IsLoaded
		{
			get
			{
				return this._cylinderModule.AmmoStored > 0;
			}
		}

		private float DoubleActionTime
		{
			get
			{
				return this._baseDoubleActionTime / base.Firearm.AttachmentsValue(AttachmentParam.DoubleActionSpeedMultiplier);
			}
		}

		private float TimeBetweenShots
		{
			get
			{
				return this._basePostShotCooldown / base.Firearm.AttachmentsValue(AttachmentParam.FireRateMultiplier);
			}
		}

		private bool OnCooldown
		{
			get
			{
				return !this._clientShotCooldown.Ready || !this._serverShotCooldown.Ready;
			}
		}

		private bool IsCocking
		{
			get
			{
				return this._cockingBusy || this._cockRequestTimer.Busy;
			}
		}

		public static bool GetCocked(ushort serial)
		{
			return DoubleActionModule.CockedHashes.Contains(serial);
		}

		public void TriggerDecocking(int? nextTriggerHash = null)
		{
			this._audioModule.PlayQuiet(this._decockingClip);
			this._cockingBusy = true;
			this._afterDecockTrigger = nextTriggerHash;
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.DeCock, false);
		}

		public void TriggerCocking()
		{
			this._audioModule.PlayNormal(this._cockingClip);
			this._cockingBusy = true;
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.Cock, false);
			if (base.IsLocalPlayer)
			{
				this._cylinderModule.ClientHoldPrediction();
			}
		}

		[ExposedFirearmEvent]
		public void SetCocked(bool value)
		{
			this.Cocked = value;
		}

		[ExposedFirearmEvent]
		public void OnDecockingAnimComplete()
		{
			this._cockingBusy = false;
			if (this._afterDecockTrigger == null)
			{
				return;
			}
			base.Firearm.AnimSetTrigger(this._afterDecockTrigger.Value, false);
			this._afterDecockTrigger = null;
		}

		[ExposedFirearmEvent]
		public void OnCockingAnimComplete()
		{
			this._cockingBusy = false;
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			switch (this.DecodeHeader(reader))
			{
			case DoubleActionModule.MessageType.RpcNewPlayerResync:
				foreach (ushort num in DoubleActionModule.CockedHashes)
				{
					Action<ushort, bool> onCockedChanged = DoubleActionModule.OnCockedChanged;
					if (onCockedChanged != null)
					{
						onCockedChanged(num, false);
					}
				}
				DoubleActionModule.CockedHashes.Clear();
				DoubleActionModule.CockedHashes.EnsureCapacity(reader.Remaining / 2);
				while (reader.Remaining > 0)
				{
					ushort num2 = reader.ReadUShort();
					DoubleActionModule.CockedHashes.Add(num2);
					Action<ushort, bool> onCockedChanged2 = DoubleActionModule.OnCockedChanged;
					if (onCockedChanged2 != null)
					{
						onCockedChanged2(num2, true);
					}
				}
				return;
			case DoubleActionModule.MessageType.RpcSetCockedTrue:
			{
				DoubleActionModule.CockedHashes.Add(serial);
				Action<ushort, bool> onCockedChanged3 = DoubleActionModule.OnCockedChanged;
				if (onCockedChanged3 == null)
				{
					return;
				}
				onCockedChanged3(serial, true);
				return;
			}
			case DoubleActionModule.MessageType.RpcSetCockedFalse:
			{
				DoubleActionModule.CockedHashes.Remove(serial);
				Action<ushort, bool> onCockedChanged4 = DoubleActionModule.OnCockedChanged;
				if (onCockedChanged4 == null)
				{
					return;
				}
				onCockedChanged4(serial, false);
				return;
			}
			case DoubleActionModule.MessageType.RpcFire:
				ShotEventManager.Trigger(new BulletShotEvent(new ItemIdentifier(base.Firearm)));
				return;
			default:
				return;
			}
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			switch (this.DecodeHeader(reader))
			{
			case DoubleActionModule.MessageType.RpcFire:
				if (base.IsSpectator)
				{
					this.PlayFireAnims(FirearmAnimatorHashes.Fire);
					this._triggerPull.Reset();
					return;
				}
				break;
			case DoubleActionModule.MessageType.RpcDryFire:
				if (base.IsSpectator)
				{
					this.PlayFireAnims(FirearmAnimatorHashes.DryFire);
					this._triggerPull.Reset();
				}
				break;
			case DoubleActionModule.MessageType.CmdUpdatePulling:
			case DoubleActionModule.MessageType.CmdShoot:
				break;
			case DoubleActionModule.MessageType.StartPulling:
				if (base.IsSpectator)
				{
					this._triggerPull.Pull(this.DoubleActionTime);
					return;
				}
				break;
			case DoubleActionModule.MessageType.StartCocking:
				this.TriggerCocking();
				return;
			default:
				return;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (base.PrimaryActionBlocked)
			{
				return;
			}
			switch (this.DecodeHeader(reader))
			{
			case DoubleActionModule.MessageType.CmdShoot:
				this.Fire(reader);
				this._cylinderModule.ServerResync();
				break;
			case DoubleActionModule.MessageType.StartPulling:
				if (!this._serverPullTokenUsed)
				{
					this._serverPullTokenUsed = true;
					this._audioModule.PlayQuiet(this._doubleActionClip);
					double targetTime = NetworkTime.time + (double)this.DoubleActionTime;
					this.SendRpc(DoubleActionModule.MessageType.StartPulling, delegate(NetworkWriter x)
					{
						x.WriteDouble(targetTime);
					}, true);
					return;
				}
				break;
			case DoubleActionModule.MessageType.StartCocking:
				if (!base.Firearm.AnyModuleBusy(this) && !this.Cocked && !this._cockingBusy)
				{
					this.SendRpc(DoubleActionModule.MessageType.StartCocking, null, true);
					return;
				}
				break;
			default:
				return;
			}
		}

		protected override void OnInit()
		{
			base.OnInit();
			this._clientCocked = new ClientPredictedValue<bool>(() => DoubleActionModule.GetCocked(base.ItemSerial));
			if (!base.Firearm.TryGetModules(out this._audioModule, out this._cylinderModule, out this._hitregModule))
			{
				throw new InvalidOperationException(string.Concat(new string[]
				{
					"The ",
					base.Firearm.name,
					" is missing one or more essential modules (required by ",
					base.name,
					")."
				}));
			}
			this._audioModule.RegisterClip(this._dryFireClip);
			this._audioModule.RegisterClip(this._doubleActionClip);
			this._audioModule.RegisterClip(this._cockingClip);
			this._audioModule.RegisterClip(this._decockingClip);
			foreach (AudioClip audioClip in this._fireClips)
			{
				this._audioModule.RegisterClip(audioClip);
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			DoubleActionModule.CockedHashes.Clear();
		}

		internal override void ServerOnPlayerConnected(ReferenceHub hub, bool firstModule)
		{
			base.ServerOnPlayerConnected(hub, firstModule);
			if (!firstModule)
			{
				return;
			}
			this.SendRpc(hub, delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(DoubleActionModule.MessageType.RpcNewPlayerResync);
				DoubleActionModule.CockedHashes.ForEach(delegate(ushort x)
				{
					writer.WriteUShort(x);
				});
			});
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			this._cockingBusy = false;
			this._cockingKeyHoldTime = 0f;
			this._serverPullTokenUsed = false;
			this._triggerPull.Reset();
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			if (!base.IsServer)
			{
				return;
			}
			if (this.Cocked)
			{
				this.SendRpc(DoubleActionModule.MessageType.RpcSetCockedTrue, null, true);
				return;
			}
			this.SendRpc(DoubleActionModule.MessageType.RpcSetCockedFalse, null, true);
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			this._clientShotCooldown.Update();
			this._serverShotCooldown.Update();
			if (base.IsLocalPlayer)
			{
				this.UpdateInputs();
				if (this._triggerPull.IsPulling && (this.TriggerPullProgress >= 1f || this.Cocked))
				{
					this.Fire(null);
				}
			}
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.IsCocked, this.Cocked, false);
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.TriggerState, this.TriggerPullProgress, false);
		}

		private void UpdateInputs()
		{
			if (base.Firearm.AnyModuleBusy(null))
			{
				return;
			}
			if (base.PrimaryActionBlocked)
			{
				return;
			}
			if (base.GetAction(ActionName.WeaponAlt))
			{
				this._cockingKeyHoldTime += Time.deltaTime;
			}
			else if (this._cockingKeyHoldTime > 0f)
			{
				if (this._cockingKeyHoldTime < 0.5f)
				{
					this._cockRequestTimer.Trigger();
					this.SendCmd(DoubleActionModule.MessageType.StartCocking, null);
				}
				this._cockingKeyHoldTime = 0f;
			}
			ITriggerControllerModule triggerControllerModule;
			if (base.Firearm.TryGetModule(out triggerControllerModule, true) && triggerControllerModule.TriggerHeld)
			{
				this._triggerPull.Pull(this.DoubleActionTime);
				if (!this.Cocked)
				{
					this.SendCmd(DoubleActionModule.MessageType.StartPulling, null);
					this._audioModule.PlayClientside(this._doubleActionClip);
				}
			}
		}

		private unsafe void Fire(NetworkReader extraData)
		{
			if (base.IsLocalPlayer)
			{
				this._triggerPull.Reset();
				this._clientShotCooldown.Trigger(this.TimeBetweenShots);
			}
			else
			{
				if (!this._serverShotCooldown.Ready)
				{
					return;
				}
				this._serverPullTokenUsed = false;
				this._serverShotCooldown.Trigger(this.TimeBetweenShots);
			}
			if (!this.Cocked)
			{
				this._cylinderModule.RotateCylinder(1);
			}
			else
			{
				this.Cocked = false;
			}
			CylinderAmmoModule.Chamber chamber = *this._cylinderModule.Chambers[0];
			if (chamber.ContextState == CylinderAmmoModule.ChamberState.Live)
			{
				this.FireLive(chamber, extraData);
			}
			else
			{
				this.FireDry();
			}
			if (base.IsLocalPlayer && !base.IsServer)
			{
				this.SendCmd(DoubleActionModule.MessageType.CmdShoot, delegate(NetworkWriter x)
				{
					x.WriteBacktrackData(new ShotBacktrackData(base.Firearm));
				});
			}
		}

		private void FireLive(CylinderAmmoModule.Chamber chamber, NetworkReader extraData)
		{
			BulletShotEvent shotEvent = new BulletShotEvent(new ItemIdentifier(base.Firearm));
			if (base.IsServer)
			{
				PlayerShootingWeaponEventArgs playerShootingWeaponEventArgs = new PlayerShootingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnShootingWeapon(playerShootingWeaponEventArgs);
				if (!playerShootingWeaponEventArgs.IsAllowed)
				{
					return;
				}
				(base.IsLocalPlayer ? new ShotBacktrackData(base.Firearm) : new ShotBacktrackData(extraData)).ProcessShot(base.Firearm, delegate(ReferenceHub target)
				{
					this._hitregModule.Fire(target, shotEvent);
				});
				this.SendRpc((ReferenceHub hub) => hub != this.Firearm.Owner && !hub.isLocalPlayer, delegate(NetworkWriter x)
				{
					x.WriteSubheader(DoubleActionModule.MessageType.RpcFire);
				});
				PlayerEvents.OnShotWeapon(new PlayerShotWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			float num = base.Firearm.AttachmentsValue(AttachmentParam.ShotClipIdOverride);
			this._audioModule.PlayGunshot(this._fireClips[(int)num]);
			chamber.ContextState = CylinderAmmoModule.ChamberState.Discharged;
			ShotEventManager.Trigger(shotEvent);
			this.PlayFireAnims(FirearmAnimatorHashes.Fire);
		}

		private void FireDry()
		{
			if (base.IsServer)
			{
				PlayerDryFiringWeaponEventArgs playerDryFiringWeaponEventArgs = new PlayerDryFiringWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnDryFiringWeapon(playerDryFiringWeaponEventArgs);
				if (!playerDryFiringWeaponEventArgs.IsAllowed)
				{
					return;
				}
				this.SendRpc(DoubleActionModule.MessageType.RpcDryFire, null, true);
				PlayerEvents.OnDryFiredWeapon(new PlayerDryFiredWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			if (base.IsLocalPlayer)
			{
				this.PlayFireAnims(FirearmAnimatorHashes.DryFire);
			}
			this._audioModule.PlayNormal(this._dryFireClip);
		}

		private void PlayFireAnims(int hash)
		{
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, global::UnityEngine.Random.value, true);
			base.Firearm.AnimSetTrigger(hash, false);
		}

		private DoubleActionModule.MessageType DecodeHeader(NetworkReader reader)
		{
			return (DoubleActionModule.MessageType)reader.ReadByte();
		}

		private void SendRpc(DoubleActionModule.MessageType header, Action<NetworkWriter> writerFunc = null, bool toAll = true)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(header);
				Action<NetworkWriter> writerFunc2 = writerFunc;
				if (writerFunc2 == null)
				{
					return;
				}
				writerFunc2(x);
			}, toAll);
		}

		private void SendCmd(DoubleActionModule.MessageType header, Action<NetworkWriter> writerFunc = null)
		{
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(header);
				Action<NetworkWriter> writerFunc2 = writerFunc;
				if (writerFunc2 == null)
				{
					return;
				}
				writerFunc2(x);
			});
		}

		private static readonly HashSet<ushort> CockedHashes = new HashSet<ushort>();

		private const float CockingKeyMaxHoldTime = 0.5f;

		[SerializeField]
		private float _baseDoubleActionTime;

		[SerializeField]
		private float _basePostShotCooldown;

		[SerializeField]
		private AudioClip[] _fireClips;

		[SerializeField]
		private AudioClip _dryFireClip;

		[SerializeField]
		private AudioClip _doubleActionClip;

		[SerializeField]
		private AudioClip _cockingClip;

		[SerializeField]
		private AudioClip _decockingClip;

		private float _cockingKeyHoldTime;

		private int? _afterDecockTrigger;

		private bool _cockingBusy;

		private bool _serverPullTokenUsed;

		private ClientPredictedValue<bool> _clientCocked;

		private AudioModule _audioModule;

		private CylinderAmmoModule _cylinderModule;

		private IHitregModule _hitregModule;

		private readonly DoubleActionModule.TriggerPull _triggerPull = new DoubleActionModule.TriggerPull();

		private readonly ClientRequestTimer _cockRequestTimer = new ClientRequestTimer();

		private readonly FullAutoRateLimiter _clientShotCooldown = new FullAutoRateLimiter();

		private readonly FullAutoRateLimiter _serverShotCooldown = new FullAutoRateLimiter();

		private enum MessageType : byte
		{
			RpcNewPlayerResync,
			RpcSetCockedTrue,
			RpcSetCockedFalse,
			RpcFire,
			RpcDryFire,
			CmdUpdatePulling,
			CmdShoot,
			StartPulling,
			StartCocking
		}

		private class TriggerPull
		{
			private double InverseLerpTime
			{
				get
				{
					return MoreMath.InverseLerp(this._pullStartTime, this._pullEndTime, NetworkTime.time);
				}
			}

			public float PullProgress
			{
				get
				{
					if (!this.IsPulling)
					{
						return 0f;
					}
					return (float)this.InverseLerpTime;
				}
			}

			public bool IsPulling { get; private set; }

			public void Pull(float durationSeconds)
			{
				this.IsPulling = true;
				this._pullStartTime = NetworkTime.time;
				this._pullEndTime = NetworkTime.time + (double)durationSeconds;
			}

			public void Reset()
			{
				this.IsPulling = false;
			}

			private double _pullStartTime;

			private double _pullEndTime;
		}
	}
}

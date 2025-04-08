using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public abstract class AnimatorReloaderModuleBase : ModuleBase, IReloaderModule, IBusyIndicatorModule
	{
		public virtual bool IsBusy
		{
			get
			{
				return this.IsReloading || this.IsUnloading || this._requestTimer.Busy;
			}
		}

		public bool IsReloading { get; protected set; }

		public bool IsUnloading { get; protected set; }

		[ExposedFirearmEvent]
		public void StopReloadingAndUnloading()
		{
			FirearmEvent currentlyInvokedEvent = FirearmEvent.CurrentlyInvokedEvent;
			if (currentlyInvokedEvent != null)
			{
				Animator rawAnimator = currentlyInvokedEvent.LastInvocation.RawAnimator;
				int layer = currentlyInvokedEvent.LastInvocation.Layer;
				if (rawAnimator.IsInTransition(layer) && rawAnimator.GetNextAnimatorStateInfo(layer).tagHash == FirearmAnimatorHashes.Reload)
				{
					return;
				}
			}
			if (this.IsReloading)
			{
				PlayerEvents.OnReloadedWeapon(new PlayerReloadedWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			if (this.IsUnloading)
			{
				PlayerEvents.OnUnloadedWeapon(new PlayerUnloadedWeaponEventArgs(base.Firearm.Owner, base.Firearm));
			}
			this.IsReloading = false;
			this.IsUnloading = false;
			if (!base.IsServer)
			{
				return;
			}
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop);
			}, true);
		}

		public bool GetDisplayReloadingOrUnloading(ushort serial)
		{
			AnimatorReloaderModuleBase.LastRpcInfo lastRpcInfo;
			return AnimatorReloaderModuleBase.LastReceivedRpcs.TryGetValue(serial, out lastRpcInfo) && lastRpcInfo.Header != AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop;
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			AnimatorReloaderModuleBase.ReloaderMessageHeader header = (AnimatorReloaderModuleBase.ReloaderMessageHeader)reader.ReadByte();
			if (!this.TryContinueDeserialization(reader, base.ItemSerial, header, AutosyncMessageType.Cmd))
			{
				return;
			}
			AnimatorReloaderModuleBase.ReloaderMessageHeader header2 = header;
			bool flag;
			if (header2 != AnimatorReloaderModuleBase.ReloaderMessageHeader.Reload)
			{
				if (header2 != AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload)
				{
					return;
				}
				flag = IReloadUnloadValidatorModule.ValidateUnload(base.Firearm);
			}
			else
			{
				flag = IReloadUnloadValidatorModule.ValidateReload(base.Firearm);
			}
			flag = flag && !base.Firearm.AnyModuleBusy(null);
			if (header == AnimatorReloaderModuleBase.ReloaderMessageHeader.Reload)
			{
				PlayerReloadingWeaponEventArgs playerReloadingWeaponEventArgs = new PlayerReloadingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnReloadingWeapon(playerReloadingWeaponEventArgs);
				if (!playerReloadingWeaponEventArgs.IsAllowed)
				{
					flag = false;
				}
			}
			else if (header == AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload)
			{
				PlayerUnloadingWeaponEventArgs playerUnloadingWeaponEventArgs = new PlayerUnloadingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
				PlayerEvents.OnUnloadingWeapon(playerUnloadingWeaponEventArgs);
				if (!playerUnloadingWeaponEventArgs.IsAllowed)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.RequestRejected);
				}, false);
				if (!this.IsReloading && !this.IsUnloading)
				{
					this.SendRpc(delegate(NetworkWriter x)
					{
						x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop);
					}, true);
				}
				return;
			}
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(header);
				if (!this._randomize)
				{
					return;
				}
				int num = global::UnityEngine.Random.Range(0, 256);
				writer.WriteByte((byte)num);
			}, true);
		}

		public override void ClientProcessRpcInstance(NetworkReader reader)
		{
			base.ClientProcessRpcInstance(reader);
			AnimatorReloaderModuleBase.ReloaderMessageHeader reloaderMessageHeader = (AnimatorReloaderModuleBase.ReloaderMessageHeader)reader.ReadByte();
			if (!this.TryContinueDeserialization(reader, base.ItemSerial, reloaderMessageHeader, AutosyncMessageType.RpcInstance))
			{
				return;
			}
			switch (reloaderMessageHeader)
			{
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Reload:
				if (!this._randomize)
				{
					goto IL_0064;
				}
				break;
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload:
				if (!this._randomize)
				{
					goto IL_0064;
				}
				break;
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop:
				goto IL_0064;
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.RequestRejected:
				this._requestTimer.Reset();
				return;
			default:
				goto IL_0064;
			}
			this.Randomize(reader.ReadByte());
			IL_0064:
			this.IsReloading = false;
			this.IsUnloading = false;
			this._targetLayer = null;
			switch (reloaderMessageHeader)
			{
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Reload:
				this.IsReloading = true;
				this.StartReloading();
				this._requestTimer.Reset();
				return;
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload:
				this.IsUnloading = true;
				this.StartUnloading();
				this._requestTimer.Reset();
				return;
			case AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop:
				this.OnStopReloadingAndUnloading();
				return;
			default:
				return;
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			AnimatorReloaderModuleBase.ReloaderMessageHeader reloaderMessageHeader = (AnimatorReloaderModuleBase.ReloaderMessageHeader)reader.ReadByte();
			if (!this.TryContinueDeserialization(reader, serial, reloaderMessageHeader, AutosyncMessageType.RpcTemplate))
			{
				return;
			}
			if (reloaderMessageHeader <= AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop)
			{
				AnimatorReloaderModuleBase.LastRpcInfo orAdd = AnimatorReloaderModuleBase.LastReceivedRpcs.GetOrAdd(serial, () => new AnimatorReloaderModuleBase.LastRpcInfo());
				orAdd.NetTime = NetworkTime.time;
				orAdd.Header = reloaderMessageHeader;
			}
		}

		internal override void OnClientReady()
		{
			base.OnClientReady();
			AnimatorReloaderModuleBase.LastReceivedRpcs.Clear();
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (this.IsReloading || this.IsUnloading)
			{
				this.DetectEnd();
			}
			if (!base.IsLocalPlayer)
			{
				return;
			}
			bool isHoldingKey = this._isHoldingKey;
			if (base.GetActionDown(ActionName.Reload))
			{
				this._keyHoldTime = 0f;
				this._isHoldingKey = true;
			}
			else if (this._isHoldingKey && !base.GetAction(ActionName.Reload))
			{
				this._isHoldingKey = false;
			}
			if (!this._isHoldingKey)
			{
				if (isHoldingKey)
				{
					this.ClientTryReload();
				}
				return;
			}
			this._keyHoldTime += Time.deltaTime;
			if (this._keyHoldTime < 1f)
			{
				return;
			}
			this._isHoldingKey = false;
			this.ClientTryUnload();
		}

		internal override void OnHolstered()
		{
			base.OnHolstered();
			if (base.IsServer && (this.IsReloading || this.IsUnloading))
			{
				this.StopReloadingAndUnloading();
			}
		}

		protected abstract void StartReloading();

		protected abstract void StartUnloading();

		protected virtual void OnStopReloadingAndUnloading()
		{
		}

		protected virtual void Randomize(byte randomByte)
		{
			float num = (float)randomByte / 255f;
			base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, num, false);
		}

		protected virtual MessageInterceptionResult InterceptMessage(NetworkReader reader, ushort serial, AnimatorReloaderModuleBase.ReloaderMessageHeader header, AutosyncMessageType scenario)
		{
			return MessageInterceptionResult.Continue;
		}

		protected virtual void ClientOnRequestSent()
		{
			this._requestTimer.Trigger();
		}

		protected virtual void OnAnimEndDetected()
		{
			this.StopReloadingAndUnloading();
		}

		private bool TryContinueDeserialization(NetworkReader reader, ushort serial, AnimatorReloaderModuleBase.ReloaderMessageHeader header, AutosyncMessageType scenario)
		{
			int position = reader.Position;
			MessageInterceptionResult messageInterceptionResult = this.InterceptMessage(reader, serial, header, scenario);
			switch (messageInterceptionResult)
			{
			case MessageInterceptionResult.Continue:
				return true;
			case MessageInterceptionResult.ResetAndContinue:
				reader.Position = position;
				return true;
			case MessageInterceptionResult.Stop:
				return false;
			default:
				throw new InvalidOperationException("Unknown interception result: " + messageInterceptionResult.ToString());
			}
		}

		private void ClientTryReload()
		{
			if (!IReloadUnloadValidatorModule.ValidateReload(base.Firearm))
			{
				return;
			}
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Reload);
			});
			this.ClientOnRequestSent();
		}

		private void ClientTryUnload()
		{
			if (!IReloadUnloadValidatorModule.ValidateUnload(base.Firearm))
			{
				return;
			}
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload);
			});
			this.ClientOnRequestSent();
		}

		private void DetectEnd()
		{
			if (this._targetLayer == null)
			{
				int layerCount = base.Firearm.ServerSideAnimator.layerCount;
				for (int i = 0; i < layerCount; i++)
				{
					if (base.Firearm.AnimGetCurStateInfo(i).tagHash == FirearmAnimatorHashes.Reload)
					{
						this._targetLayer = new int?(i);
						return;
					}
				}
				return;
			}
			if (base.Firearm.AnimGetCurStateInfo(this._targetLayer.Value).tagHash == FirearmAnimatorHashes.Reload)
			{
				return;
			}
			this.OnAnimEndDetected();
		}

		public const float UnloadHoldTime = 1f;

		[SerializeField]
		private bool _randomize;

		private int? _targetLayer;

		private float _keyHoldTime;

		private bool _isHoldingKey;

		private readonly ClientRequestTimer _requestTimer = new ClientRequestTimer();

		private static readonly Dictionary<ushort, AnimatorReloaderModuleBase.LastRpcInfo> LastReceivedRpcs = new Dictionary<ushort, AnimatorReloaderModuleBase.LastRpcInfo>();

		private class LastRpcInfo
		{
			public AnimatorReloaderModuleBase.ReloaderMessageHeader Header;

			public double NetTime;
		}

		protected enum ReloaderMessageHeader : byte
		{
			Reload,
			Unload,
			Stop,
			RequestRejected,
			Custom
		}
	}
}

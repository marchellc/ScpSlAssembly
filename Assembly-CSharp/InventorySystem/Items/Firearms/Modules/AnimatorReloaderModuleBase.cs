using System;
using System.Collections.Generic;
using InventorySystem.Items.Armor;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Firearms.Modules.Misc;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public abstract class AnimatorReloaderModuleBase : ModuleBase, IReloaderModule, IBusyIndicatorModule
{
	private class LastRpcInfo
	{
		public ReloaderMessageHeader Header;

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

	public const float UnloadHoldTime = 1f;

	[SerializeField]
	private bool _randomize;

	private int? _targetLayer;

	private float _keyHoldTime;

	private bool _isHoldingKey;

	private readonly ClientRequestTimer _requestTimer = new ClientRequestTimer();

	private static readonly Dictionary<ushort, LastRpcInfo> LastReceivedRpcs = new Dictionary<ushort, LastRpcInfo>();

	public virtual bool IsBusy
	{
		get
		{
			if (!this.IsReloading && !this.IsUnloading)
			{
				if (base.IsLocalPlayer)
				{
					return this._requestTimer.Busy;
				}
				return false;
			}
			return true;
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
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(ReloaderMessageHeader.Stop);
			});
		}
	}

	public bool GetDisplayReloadingOrUnloading(ushort serial)
	{
		if (AnimatorReloaderModuleBase.LastReceivedRpcs.TryGetValue(serial, out var value))
		{
			return value.Header != ReloaderMessageHeader.Stop;
		}
		return false;
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		ReloaderMessageHeader header = (ReloaderMessageHeader)reader.ReadByte();
		if (!this.TryContinueDeserialization(reader, base.ItemSerial, header, AutosyncMessageType.Cmd))
		{
			return;
		}
		bool flag;
		switch (header)
		{
		default:
			return;
		case ReloaderMessageHeader.Reload:
			flag = IReloadUnloadValidatorModule.ValidateReload(base.Firearm);
			break;
		case ReloaderMessageHeader.Unload:
			flag = IReloadUnloadValidatorModule.ValidateUnload(base.Firearm);
			break;
		}
		flag = flag && !base.Firearm.AnyModuleBusy();
		if (header == ReloaderMessageHeader.Reload)
		{
			PlayerReloadingWeaponEventArgs e = new PlayerReloadingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnReloadingWeapon(e);
			if (!e.IsAllowed)
			{
				flag = false;
			}
		}
		else if (header == ReloaderMessageHeader.Unload)
		{
			PlayerUnloadingWeaponEventArgs e2 = new PlayerUnloadingWeaponEventArgs(base.Firearm.Owner, base.Firearm);
			PlayerEvents.OnUnloadingWeapon(e2);
			if (!e2.IsAllowed)
			{
				flag = false;
			}
		}
		if (!flag)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteSubheader(ReloaderMessageHeader.RequestRejected);
			}, toAll: false);
			if (!this.IsReloading && !this.IsUnloading)
			{
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(ReloaderMessageHeader.Stop);
				});
			}
			return;
		}
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(header);
			if (this._randomize)
			{
				int num = UnityEngine.Random.Range(0, 256);
				writer.WriteByte((byte)num);
			}
		});
	}

	public override void ClientProcessRpcInstance(NetworkReader reader)
	{
		base.ClientProcessRpcInstance(reader);
		ReloaderMessageHeader reloaderMessageHeader = (ReloaderMessageHeader)reader.ReadByte();
		if (!this.TryContinueDeserialization(reader, base.ItemSerial, reloaderMessageHeader, AutosyncMessageType.RpcInstance))
		{
			return;
		}
		switch (reloaderMessageHeader)
		{
		case ReloaderMessageHeader.RequestRejected:
			this._requestTimer.Reset();
			return;
		case ReloaderMessageHeader.Unload:
			if (!this._randomize)
			{
				break;
			}
			goto IL_0058;
		case ReloaderMessageHeader.Reload:
			{
				if (!this._randomize)
				{
					break;
				}
				goto IL_0058;
			}
			IL_0058:
			this.Randomize(reader.ReadByte());
			break;
		}
		this.IsReloading = false;
		this.IsUnloading = false;
		this._targetLayer = null;
		switch (reloaderMessageHeader)
		{
		case ReloaderMessageHeader.Reload:
			this.IsReloading = true;
			this.StartReloading();
			this._requestTimer.Reset();
			break;
		case ReloaderMessageHeader.Unload:
			this.IsUnloading = true;
			this.StartUnloading();
			this._requestTimer.Reset();
			break;
		case ReloaderMessageHeader.Stop:
			this.OnStopReloadingAndUnloading();
			break;
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		ReloaderMessageHeader reloaderMessageHeader = (ReloaderMessageHeader)reader.ReadByte();
		if (this.TryContinueDeserialization(reader, serial, reloaderMessageHeader, AutosyncMessageType.RpcTemplate) && reloaderMessageHeader <= ReloaderMessageHeader.Stop)
		{
			LastRpcInfo orAdd = AnimatorReloaderModuleBase.LastReceivedRpcs.GetOrAdd(serial, () => new LastRpcInfo());
			orAdd.NetTime = NetworkTime.time;
			orAdd.Header = reloaderMessageHeader;
		}
	}

	public void ClientTryReload()
	{
		if (IReloadUnloadValidatorModule.ValidateReload(base.Firearm))
		{
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(ReloaderMessageHeader.Reload);
			});
			this.ClientOnRequestSent();
		}
	}

	public void ClientTryUnload()
	{
		if (IReloadUnloadValidatorModule.ValidateUnload(base.Firearm))
		{
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteSubheader(ReloaderMessageHeader.Unload);
			});
			this.ClientOnRequestSent();
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
		if (!base.IsControllable)
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
		if (this._isHoldingKey)
		{
			this._keyHoldTime += Time.deltaTime;
			if (!(this._keyHoldTime < 1f))
			{
				this._isHoldingKey = false;
				this.ClientTryUnload();
			}
		}
		else if (isHoldingKey)
		{
			this.ClientTryReload();
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		if (base.IsServer)
		{
			if (this.IsReloading || this.IsUnloading)
			{
				this.StopReloadingAndUnloading();
			}
			BodyArmorUtils.SetPlayerDirty(base.Item.Owner);
		}
	}

	protected abstract void StartReloading();

	protected abstract void StartUnloading();

	protected virtual void OnStopReloadingAndUnloading()
	{
	}

	protected virtual void Randomize(byte randomByte)
	{
		float f = (float)(int)randomByte / 255f;
		base.Firearm.AnimSetFloat(FirearmAnimatorHashes.Random, f);
	}

	protected virtual MessageInterceptionResult InterceptMessage(NetworkReader reader, ushort serial, ReloaderMessageHeader header, AutosyncMessageType scenario)
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

	private bool TryContinueDeserialization(NetworkReader reader, ushort serial, ReloaderMessageHeader header, AutosyncMessageType scenario)
	{
		int position = reader.Position;
		MessageInterceptionResult messageInterceptionResult = this.InterceptMessage(reader, serial, header, scenario);
		switch (messageInterceptionResult)
		{
		case MessageInterceptionResult.ResetAndContinue:
			reader.Position = position;
			return true;
		case MessageInterceptionResult.Continue:
			return true;
		case MessageInterceptionResult.Stop:
			return false;
		default:
			throw new InvalidOperationException("Unknown interception result: " + messageInterceptionResult);
		}
	}

	private void DetectEnd()
	{
		if (this._targetLayer.HasValue)
		{
			if (base.Firearm.AnimGetCurStateInfo(this._targetLayer.Value).tagHash != FirearmAnimatorHashes.Reload)
			{
				this.OnAnimEndDetected();
			}
			return;
		}
		int layerCount = base.Firearm.ServerSideAnimator.layerCount;
		for (int i = 0; i < layerCount; i++)
		{
			if (base.Firearm.AnimGetCurStateInfo(i).tagHash == FirearmAnimatorHashes.Reload)
			{
				this._targetLayer = i;
				break;
			}
		}
	}
}

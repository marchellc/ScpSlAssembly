using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules;

public class AnimationToggleableReloaderModule : AnimatorReloaderModuleBase
{
	private enum RpcType
	{
		LoadableAmmoSync,
		StopAnimations
	}

	private static readonly ActionName[] CancelActions = new ActionName[2]
	{
		ActionName.Shoot,
		ActionName.Zoom
	};

	private static readonly Dictionary<ushort, int> SyncLoadableAmmo = new Dictionary<ushort, int>();

	private readonly RateLimiter _postCancelLimiter = new RateLimiter(0.7f);

	private int _prevLoadableAmmo;

	private int ServerLoadableAmmo
	{
		get
		{
			if (!base.Firearm.TryGetModule<IPrimaryAmmoContainerModule>(out var module))
			{
				return 0;
			}
			int a = module.AmmoMax - module.AmmoStored;
			int curAmmo = base.Firearm.OwnerInventory.GetCurAmmo(module.AmmoType);
			return Mathf.Min(a, curAmmo);
		}
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.IsControllable)
		{
			this.UpdateControls();
		}
		if (base.IsServer)
		{
			this.UpdateServer();
		}
		base.Firearm.AnimSetInt(FirearmAnimatorHashes.LoadableAmmo, this._prevLoadableAmmo);
	}

	internal override void OnEquipped()
	{
		base.OnEquipped();
		AnimationToggleableReloaderModule.SyncLoadableAmmo.TryGetValue(base.ItemSerial, out this._prevLoadableAmmo);
	}

	protected override MessageInterceptionResult InterceptMessage(NetworkReader reader, ushort serial, ReloaderMessageHeader header, AutosyncMessageType scenario)
	{
		if (scenario == AutosyncMessageType.Cmd)
		{
			switch (header)
			{
			case ReloaderMessageHeader.Reload:
			case ReloaderMessageHeader.Unload:
				if (!this._postCancelLimiter.InstantReady)
				{
					return MessageInterceptionResult.Stop;
				}
				return MessageInterceptionResult.Continue;
			case ReloaderMessageHeader.Stop:
				this.SendRpc(delegate(NetworkWriter x)
				{
					x.WriteSubheader(ReloaderMessageHeader.Custom);
					x.WriteSubheader(RpcType.StopAnimations);
				});
				return MessageInterceptionResult.Stop;
			default:
				return MessageInterceptionResult.Continue;
			}
		}
		if (header == ReloaderMessageHeader.Custom && (scenario == AutosyncMessageType.RpcInstance || scenario == AutosyncMessageType.RpcTemplate))
		{
			this.ProcessCustomRpc(reader, serial, (RpcType)reader.ReadByte(), scenario);
			return MessageInterceptionResult.Stop;
		}
		return base.InterceptMessage(reader, serial, header, scenario);
	}

	protected override void StartReloading()
	{
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, b: true);
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload);
		if (base.IsServer)
		{
			this.ServerSendLoadableAmmo();
		}
	}

	protected override void StartUnloading()
	{
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Unload, b: true);
		base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload);
		if (base.IsServer)
		{
			this.ServerSendLoadableAmmo();
		}
	}

	protected override void OnStopReloadingAndUnloading()
	{
		this.StopAnimations();
		if (this._postCancelLimiter.InstantReady)
		{
			this._postCancelLimiter.RegisterInput();
		}
	}

	private void ProcessCustomRpc(NetworkReader reader, ushort serial, RpcType rpc, AutosyncMessageType messageType)
	{
		bool flag = messageType == AutosyncMessageType.RpcInstance;
		switch (rpc)
		{
		case RpcType.StopAnimations:
			if (flag)
			{
				this.StopAnimations();
			}
			break;
		case RpcType.LoadableAmmoSync:
		{
			byte b = reader.ReadByte();
			AnimationToggleableReloaderModule.SyncLoadableAmmo[serial] = b;
			if (messageType == AutosyncMessageType.RpcInstance)
			{
				this._prevLoadableAmmo = b;
			}
			break;
		}
		}
	}

	private void StopAnimations()
	{
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, b: false);
		base.Firearm.AnimSetBool(FirearmAnimatorHashes.Unload, b: false);
	}

	private void UpdateControls()
	{
		if (!base.IsReloading && !base.IsUnloading)
		{
			return;
		}
		ActionName[] cancelActions = AnimationToggleableReloaderModule.CancelActions;
		foreach (ActionName action in cancelActions)
		{
			if (base.GetActionDown(action))
			{
				this.SendCmd(delegate(NetworkWriter writer)
				{
					writer.WriteSubheader(ReloaderMessageHeader.Stop);
				});
				break;
			}
		}
	}

	private void UpdateServer()
	{
		int serverLoadableAmmo = this.ServerLoadableAmmo;
		if (this._prevLoadableAmmo != serverLoadableAmmo)
		{
			this._prevLoadableAmmo = serverLoadableAmmo;
			this.ServerSendLoadableAmmo();
		}
	}

	private void ServerSendLoadableAmmo()
	{
		int clamped = Mathf.Clamp(this._prevLoadableAmmo, 0, 255);
		this.SendRpc(delegate(NetworkWriter writer)
		{
			writer.WriteSubheader(ReloaderMessageHeader.Custom);
			writer.WriteSubheader(RpcType.LoadableAmmoSync);
			writer.WriteByte((byte)clamped);
		});
	}
}

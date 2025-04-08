using System;
using System.Collections.Generic;
using InventorySystem.Items.Autosync;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules
{
	public class AnimationToggleableReloaderModule : AnimatorReloaderModuleBase
	{
		private int ServerLoadableAmmo
		{
			get
			{
				IPrimaryAmmoContainerModule primaryAmmoContainerModule;
				if (!base.Firearm.TryGetModule(out primaryAmmoContainerModule, true))
				{
					return 0;
				}
				int num = primaryAmmoContainerModule.AmmoMax - primaryAmmoContainerModule.AmmoStored;
				int curAmmo = (int)base.Firearm.OwnerInventory.GetCurAmmo(primaryAmmoContainerModule.AmmoType);
				return Mathf.Min(num, curAmmo);
			}
		}

		internal override void EquipUpdate()
		{
			base.EquipUpdate();
			if (base.IsLocalPlayer)
			{
				this.UpdateLocalPlayer();
			}
			if (base.IsServer)
			{
				this.UpdateServer();
			}
			base.Firearm.AnimSetInt(FirearmAnimatorHashes.LoadableAmmo, this._prevLoadableAmmo, false);
		}

		internal override void OnEquipped()
		{
			base.OnEquipped();
			AnimationToggleableReloaderModule.SyncLoadableAmmo.TryGetValue(base.ItemSerial, out this._prevLoadableAmmo);
		}

		protected override MessageInterceptionResult InterceptMessage(NetworkReader reader, ushort serial, AnimatorReloaderModuleBase.ReloaderMessageHeader header, AutosyncMessageType scenario)
		{
			if (scenario == AutosyncMessageType.Cmd)
			{
				if (header > AnimatorReloaderModuleBase.ReloaderMessageHeader.Unload)
				{
					if (header != AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop)
					{
						return MessageInterceptionResult.Continue;
					}
					this.SendRpc(delegate(NetworkWriter x)
					{
						x.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Custom);
						x.WriteSubheader(AnimationToggleableReloaderModule.RpcType.StopAnimations);
					}, true);
					return MessageInterceptionResult.Stop;
				}
				else
				{
					if (!this._postCancelLimiter.InstantReady)
					{
						return MessageInterceptionResult.Stop;
					}
					return MessageInterceptionResult.Continue;
				}
			}
			else
			{
				if (header == AnimatorReloaderModuleBase.ReloaderMessageHeader.Custom && (scenario == AutosyncMessageType.RpcInstance || scenario == AutosyncMessageType.RpcTemplate))
				{
					this.ProcessCustomRpc(reader, serial, (AnimationToggleableReloaderModule.RpcType)reader.ReadByte(), scenario);
					return MessageInterceptionResult.Stop;
				}
				return base.InterceptMessage(reader, serial, header, scenario);
			}
		}

		protected override void StartReloading()
		{
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, true, false);
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload, false);
			if (base.IsServer)
			{
				this.ServerSendLoadableAmmo();
			}
		}

		protected override void StartUnloading()
		{
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Unload, true, false);
			base.Firearm.AnimSetTrigger(FirearmAnimatorHashes.StartReloadOrUnload, false);
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

		private void ProcessCustomRpc(NetworkReader reader, ushort serial, AnimationToggleableReloaderModule.RpcType rpc, AutosyncMessageType messageType)
		{
			bool flag = messageType == AutosyncMessageType.RpcInstance;
			if (rpc != AnimationToggleableReloaderModule.RpcType.LoadableAmmoSync)
			{
				if (rpc == AnimationToggleableReloaderModule.RpcType.StopAnimations && flag)
				{
					this.StopAnimations();
					return;
				}
			}
			else
			{
				byte b = reader.ReadByte();
				AnimationToggleableReloaderModule.SyncLoadableAmmo[serial] = (int)b;
				if (messageType == AutosyncMessageType.RpcInstance)
				{
					this._prevLoadableAmmo = (int)b;
				}
			}
		}

		private void StopAnimations()
		{
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Reload, false, false);
			base.Firearm.AnimSetBool(FirearmAnimatorHashes.Unload, false, false);
		}

		private void UpdateLocalPlayer()
		{
			if (!base.IsReloading && !base.IsUnloading)
			{
				return;
			}
			foreach (ActionName actionName in AnimationToggleableReloaderModule.CancelActions)
			{
				if (base.GetActionDown(actionName))
				{
					this.SendCmd(delegate(NetworkWriter writer)
					{
						writer.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Stop);
					});
					return;
				}
			}
		}

		private void UpdateServer()
		{
			int serverLoadableAmmo = this.ServerLoadableAmmo;
			if (this._prevLoadableAmmo == serverLoadableAmmo)
			{
				return;
			}
			this._prevLoadableAmmo = serverLoadableAmmo;
			this.ServerSendLoadableAmmo();
		}

		private void ServerSendLoadableAmmo()
		{
			int clamped = Mathf.Clamp(this._prevLoadableAmmo, 0, 255);
			this.SendRpc(delegate(NetworkWriter writer)
			{
				writer.WriteSubheader(AnimatorReloaderModuleBase.ReloaderMessageHeader.Custom);
				writer.WriteSubheader(AnimationToggleableReloaderModule.RpcType.LoadableAmmoSync);
				writer.WriteByte((byte)clamped);
			}, true);
		}

		private static readonly ActionName[] CancelActions = new ActionName[]
		{
			ActionName.Shoot,
			ActionName.Zoom
		};

		private static readonly Dictionary<ushort, int> SyncLoadableAmmo = new Dictionary<ushort, int>();

		private readonly RateLimiter _postCancelLimiter = new RateLimiter(0.7f);

		private int _prevLoadableAmmo;

		private enum RpcType
		{
			LoadableAmmoSync,
			StopAnimations
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using InventorySystem.Items.Autosync;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Coin;

public class Coin : AutosyncItem, IItemDescription, IItemNametag
{
	[SerializeField]
	private AudioClip _firstpersonFlipSound;

	[SerializeField]
	private AudioClip _thirdpersonFlipSound;

	private readonly Stopwatch _lastUseSw = Stopwatch.StartNew();

	private const float RateLimit = 0.6f;

	public static readonly Dictionary<ushort, double> FlipTimes = new Dictionary<ushort, double>();

	private static readonly int IdleHash = Animator.StringToHash("Idle");

	private static readonly ActionName[] ActivationKeys = new ActionName[3]
	{
		ActionName.InspectItem,
		ActionName.Shoot,
		ActionName.Zoom
	};

	public override float Weight => 0.0025f;

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	public static event Action<ushort, bool> OnFlipped;

	public override void EquipUpdate()
	{
		if (!base.IsControllable || this._lastUseSw.Elapsed.TotalSeconds < 0.6000000238418579 || (base.ViewModel is AnimatedViewmodelBase animatedViewmodelBase && animatedViewmodelBase.AnimatorStateInfo(0).tagHash != Coin.IdleHash) || base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
		{
			return;
		}
		ActionName[] activationKeys = Coin.ActivationKeys;
		foreach (ActionName action in activationKeys)
		{
			if (base.GetActionDown(action))
			{
				this._lastUseSw.Restart();
				new AutosyncCmd(base.ItemId).Send();
				break;
			}
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		bool flag = reader.ReadBool();
		Coin.OnFlipped?.Invoke(serial, flag);
		Coin.FlipTimes[serial] = NetworkTime.time * (double)((!flag) ? 1 : (-1));
		if (InventoryExtensions.TryGetHubHoldingSerial(serial, out var hub))
		{
			if (hub.isLocalPlayer)
			{
				AudioSourcePoolManager.Play2D(this._firstpersonFlipSound);
			}
			else
			{
				AudioSourcePoolManager.PlayOnTransform(this._thirdpersonFlipSound, hub.transform, 5.5f);
			}
		}
	}

	internal override void OnTemplateReloaded(bool wasEverLoaded)
	{
		base.OnTemplateReloaded(wasEverLoaded);
		if (!wasEverLoaded)
		{
			CustomNetworkManager.OnClientReady += Coin.FlipTimes.Clear;
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if ((!base.Owner.isLocalPlayer && this._lastUseSw.Elapsed.TotalSeconds < 0.6000000238418579) || base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
		{
			return;
		}
		bool isTails = UnityEngine.Random.value >= 0.5f;
		PlayerFlippingCoinEventArgs e = new PlayerFlippingCoinEventArgs(base.Owner, this, isTails);
		PlayerEvents.OnFlippingCoin(e);
		if (e.IsAllowed)
		{
			isTails = e.IsTails;
			this._lastUseSw.Restart();
			NetworkWriter writer;
			using (new AutosyncRpc(base.ItemId, out writer))
			{
				writer.WriteBool(isTails);
			}
			PlayerEvents.OnFlippedCoin(new PlayerFlippedCoinEventArgs(base.Owner, this, isTails));
		}
	}
}

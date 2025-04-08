using System;
using System.Collections.Generic;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration.Holidays;
using Mirror;
using UnityEngine;

public class SnowballItem : ThrowableItem, IHolidayItem
{
	public new HolidayType[] TargetHolidays { get; } = new HolidayType[] { HolidayType.Christmas };

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		this._baseSettings = this.FullThrowSettings;
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!this.ThrowStopwatch.IsRunning)
		{
			return;
		}
		this.FullThrowSettings = ((this.ThrowStopwatch.Elapsed.TotalSeconds > 4.0) ? this._cookedSettings : this._baseSettings);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += SnowballItem.ThrownCooked.Clear;
		CustomNetworkManager.OnClientReady += SnowballItem.StartCookingTimes.Clear;
		ThrowableNetworkHandler.OnAudioMessageReceived += SnowballItem.OnMsgReceived;
		ThrowableNetworkHandler.OnServerRequestReceived += SnowballItem.OnMsgReceived;
	}

	private static void ProcessRequest(ushort serial, ThrowableNetworkHandler.RequestType request)
	{
		switch (request)
		{
		case ThrowableNetworkHandler.RequestType.BeginThrow:
			SnowballItem.StartCookingTimes[serial] = NetworkTime.time;
			return;
		case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
		case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
			if (SnowballItem.IsCooked(serial, false))
			{
				SnowballItem.ThrownCooked.Add(serial);
			}
			return;
		case ThrowableNetworkHandler.RequestType.CancelThrow:
			SnowballItem.StartCookingTimes.Remove(serial);
			return;
		default:
			return;
		}
	}

	private static void OnMsgReceived(ThrowableNetworkHandler.ThrowableItemAudioMessage msg)
	{
		SnowballItem.ProcessRequest(msg.Serial, msg.Request);
	}

	private static void OnMsgReceived(ThrowableNetworkHandler.ThrowableItemRequestMessage msg)
	{
		SnowballItem.ProcessRequest(msg.Serial, msg.Request);
	}

	public static bool IsCooked(ushort serial, bool thrown)
	{
		if (thrown)
		{
			return SnowballItem.ThrownCooked.Contains(serial);
		}
		double num;
		return SnowballItem.StartCookingTimes.TryGetValue(serial, out num) && NetworkTime.time - num > 4.0;
	}

	private static readonly Dictionary<ushort, double> StartCookingTimes = new Dictionary<ushort, double>();

	private static readonly HashSet<ushort> ThrownCooked = new HashSet<ushort>();

	public const float CookingTime = 4f;

	private ThrowableItem.ProjectileSettings _baseSettings;

	[SerializeField]
	private ThrowableItem.ProjectileSettings _cookedSettings;
}

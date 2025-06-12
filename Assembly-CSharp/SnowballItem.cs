using System.Collections.Generic;
using InventorySystem.Items;
using InventorySystem.Items.Pickups;
using InventorySystem.Items.ThrowableProjectiles;
using MapGeneration.Holidays;
using Mirror;
using UnityEngine;

public class SnowballItem : ThrowableItem, IHolidayItem
{
	private static readonly Dictionary<ushort, double> StartCookingTimes = new Dictionary<ushort, double>();

	private static readonly HashSet<ushort> ThrownCooked = new HashSet<ushort>();

	public const float CookingTime = 4f;

	private ProjectileSettings _baseSettings;

	[SerializeField]
	private ProjectileSettings _cookedSettings;

	public new HolidayType[] TargetHolidays { get; } = new HolidayType[1] { HolidayType.Christmas };

	public override void OnAdded(ItemPickupBase pickup)
	{
		base.OnAdded(pickup);
		this._baseSettings = base.FullThrowSettings;
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (base.ThrowStopwatch.IsRunning)
		{
			bool flag = base.ThrowStopwatch.Elapsed.TotalSeconds > 4.0;
			base.FullThrowSettings = (flag ? this._cookedSettings : this._baseSettings);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += SnowballItem.ThrownCooked.Clear;
		CustomNetworkManager.OnClientReady += SnowballItem.StartCookingTimes.Clear;
		ThrowableNetworkHandler.OnAudioMessageReceived += OnMsgReceived;
		ThrowableNetworkHandler.OnServerRequestReceived += OnMsgReceived;
	}

	private static void ProcessRequest(ushort serial, ThrowableNetworkHandler.RequestType request)
	{
		switch (request)
		{
		case ThrowableNetworkHandler.RequestType.BeginThrow:
			SnowballItem.StartCookingTimes[serial] = NetworkTime.time;
			break;
		case ThrowableNetworkHandler.RequestType.CancelThrow:
			SnowballItem.StartCookingTimes.Remove(serial);
			break;
		case ThrowableNetworkHandler.RequestType.ConfirmThrowWeak:
		case ThrowableNetworkHandler.RequestType.ConfirmThrowFullForce:
			if (SnowballItem.IsCooked(serial, thrown: false))
			{
				SnowballItem.ThrownCooked.Add(serial);
			}
			break;
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
		if (SnowballItem.StartCookingTimes.TryGetValue(serial, out var value))
		{
			return NetworkTime.time - value > 4.0;
		}
		return false;
	}
}

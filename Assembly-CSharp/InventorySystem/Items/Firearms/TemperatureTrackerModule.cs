using System;
using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms;

public class TemperatureTrackerModule : ModuleBase
{
	[Serializable]
	public struct TemperatureSettings
	{
		public AnimationCurve AdditionPerShotOverTemperature;

		public float HeatHalflife;
	}

	private class TemperatureRecord
	{
		public double Temperature;

		public Stopwatch LastRead;

		public TemperatureSettings Settings;
	}

	private static readonly Dictionary<ushort, TemperatureRecord> TemperatureOfFirearms = new Dictionary<ushort, TemperatureRecord>();

	private static readonly Dictionary<ItemType, TemperatureSettings> SettingsOfFirearms = new Dictionary<ItemType, TemperatureSettings>();

	private static readonly TemperatureSettings DefaultSettings = new TemperatureSettings
	{
		AdditionPerShotOverTemperature = AnimationCurve.Constant(0f, 1f, 0f),
		HeatHalflife = 1f
	};

	[SerializeField]
	private TemperatureSettings _temperatureSettings;

	public static event Action<ItemIdentifier> OnTemperatureSet;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		Inventory.OnLocalClientStarted += TemperatureTrackerModule.TemperatureOfFirearms.Clear;
		ShotEventManager.OnShot += RegisterShot;
	}

	private static void RegisterShot(ShotEvent ev)
	{
		TemperatureRecord record = TemperatureTrackerModule.GetRecord(ev.ItemId);
		float num = (float)record.Temperature;
		AnimationCurve additionPerShotOverTemperature = record.Settings.AdditionPerShotOverTemperature;
		float num2 = Mathf.Clamp01(num + additionPerShotOverTemperature.Evaluate(num));
		record.Temperature = num2;
		TemperatureTrackerModule.OnTemperatureSet?.Invoke(ev.ItemId);
	}

	private static TemperatureSettings GetSettingsForFirearm(ItemType firearmType)
	{
		if (!InventoryItemLoader.TryGetItem<Firearm>(firearmType, out var result))
		{
			return TemperatureTrackerModule.DefaultSettings;
		}
		if (!result.TryGetModule<TemperatureTrackerModule>(out var module))
		{
			return TemperatureTrackerModule.DefaultSettings;
		}
		return module._temperatureSettings;
	}

	private static TemperatureRecord CreateNewRecord(ItemIdentifier id)
	{
		if (!TemperatureTrackerModule.SettingsOfFirearms.TryGetValue(id.TypeId, out var value))
		{
			value = TemperatureTrackerModule.GetSettingsForFirearm(id.TypeId);
			TemperatureTrackerModule.SettingsOfFirearms.Add(id.TypeId, value);
		}
		return new TemperatureRecord
		{
			Temperature = 0.0,
			LastRead = Stopwatch.StartNew(),
			Settings = value
		};
	}

	private static TemperatureRecord GetRecord(ItemIdentifier firearm)
	{
		TemperatureRecord orAdd = TemperatureTrackerModule.TemperatureOfFirearms.GetOrAdd(firearm.SerialNumber, () => TemperatureTrackerModule.CreateNewRecord(firearm));
		double totalSeconds = orAdd.LastRead.Elapsed.TotalSeconds;
		if (totalSeconds > (double)Time.deltaTime)
		{
			double num = Math.Pow(0.5, totalSeconds / (double)orAdd.Settings.HeatHalflife);
			double temperature = orAdd.Temperature * num;
			orAdd.LastRead.Restart();
			orAdd.Temperature = temperature;
		}
		return orAdd;
	}

	public static float GetTemperature(ItemIdentifier firearm)
	{
		return (float)TemperatureTrackerModule.GetRecord(firearm).Temperature;
	}
}

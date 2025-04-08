using System;
using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms
{
	public class TemperatureTrackerModule : ModuleBase
	{
		public static event Action<ItemIdentifier> OnTemperatureSet;

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			Inventory.OnLocalClientStarted += TemperatureTrackerModule.TemperatureOfFirearms.Clear;
			ShotEventManager.OnShot += TemperatureTrackerModule.RegisterShot;
		}

		private static void RegisterShot(ShotEvent ev)
		{
			TemperatureTrackerModule.TemperatureRecord record = TemperatureTrackerModule.GetRecord(ev.ItemId);
			float num = (float)record.Temperature;
			AnimationCurve additionPerShotOverTemperature = record.Settings.AdditionPerShotOverTemperature;
			float num2 = Mathf.Clamp01(num + additionPerShotOverTemperature.Evaluate(num));
			record.Temperature = (double)num2;
			Action<ItemIdentifier> onTemperatureSet = TemperatureTrackerModule.OnTemperatureSet;
			if (onTemperatureSet == null)
			{
				return;
			}
			onTemperatureSet(ev.ItemId);
		}

		private static TemperatureTrackerModule.TemperatureSettings GetSettingsForFirearm(ItemType firearmType)
		{
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(firearmType, out firearm))
			{
				return TemperatureTrackerModule.DefaultSettings;
			}
			TemperatureTrackerModule temperatureTrackerModule;
			if (!firearm.TryGetModule(out temperatureTrackerModule, true))
			{
				return TemperatureTrackerModule.DefaultSettings;
			}
			return temperatureTrackerModule._temperatureSettings;
		}

		private static TemperatureTrackerModule.TemperatureRecord CreateNewRecord(ItemIdentifier id)
		{
			TemperatureTrackerModule.TemperatureSettings settingsForFirearm;
			if (!TemperatureTrackerModule.SettingsOfFirearms.TryGetValue(id.TypeId, out settingsForFirearm))
			{
				settingsForFirearm = TemperatureTrackerModule.GetSettingsForFirearm(id.TypeId);
				TemperatureTrackerModule.SettingsOfFirearms.Add(id.TypeId, settingsForFirearm);
			}
			return new TemperatureTrackerModule.TemperatureRecord
			{
				Temperature = 0.0,
				LastRead = Stopwatch.StartNew(),
				Settings = settingsForFirearm
			};
		}

		private static TemperatureTrackerModule.TemperatureRecord GetRecord(ItemIdentifier firearm)
		{
			TemperatureTrackerModule.TemperatureRecord orAdd = TemperatureTrackerModule.TemperatureOfFirearms.GetOrAdd(firearm.SerialNumber, () => TemperatureTrackerModule.CreateNewRecord(firearm));
			double totalSeconds = orAdd.LastRead.Elapsed.TotalSeconds;
			if (totalSeconds > (double)Time.deltaTime)
			{
				double num = Math.Pow(0.5, totalSeconds / (double)orAdd.Settings.HeatHalflife);
				double num2 = orAdd.Temperature * num;
				orAdd.LastRead.Restart();
				orAdd.Temperature = num2;
			}
			return orAdd;
		}

		public static float GetTemperature(ItemIdentifier firearm)
		{
			return (float)TemperatureTrackerModule.GetRecord(firearm).Temperature;
		}

		private static readonly Dictionary<ushort, TemperatureTrackerModule.TemperatureRecord> TemperatureOfFirearms = new Dictionary<ushort, TemperatureTrackerModule.TemperatureRecord>();

		private static readonly Dictionary<ItemType, TemperatureTrackerModule.TemperatureSettings> SettingsOfFirearms = new Dictionary<ItemType, TemperatureTrackerModule.TemperatureSettings>();

		private static readonly TemperatureTrackerModule.TemperatureSettings DefaultSettings = new TemperatureTrackerModule.TemperatureSettings
		{
			AdditionPerShotOverTemperature = AnimationCurve.Constant(0f, 1f, 0f),
			HeatHalflife = 1f
		};

		[SerializeField]
		private TemperatureTrackerModule.TemperatureSettings _temperatureSettings;

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

			public TemperatureTrackerModule.TemperatureSettings Settings;
		}
	}
}

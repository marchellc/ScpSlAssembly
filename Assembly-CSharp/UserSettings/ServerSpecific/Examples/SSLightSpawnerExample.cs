using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AdminToys;
using GameCore;
using Mirror;
using TMPro;
using UnityEngine;
using Utils.Networking;

namespace UserSettings.ServerSpecific.Examples
{
	public class SSLightSpawnerExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "Light Spawner";
			}
		}

		public override void Activate()
		{
			SSLightSpawnerExample._anySpawned = false;
			SSLightSpawnerExample._lightIntensity = 1f;
			SSLightSpawnerExample._lightRange = 10f;
			if (SSLightSpawnerExample._colorPresets == null)
			{
				SSLightSpawnerExample._colorPresets = new SSLightSpawnerExample.ColorPreset[]
				{
					new SSLightSpawnerExample.ColorPreset("White", Color.white),
					new SSLightSpawnerExample.ColorPreset("Black", Color.black),
					new SSLightSpawnerExample.ColorPreset("Gray", Color.gray),
					new SSLightSpawnerExample.ColorPreset("Red", Color.red),
					new SSLightSpawnerExample.ColorPreset("Green", Color.green),
					new SSLightSpawnerExample.ColorPreset("Blue", Color.blue),
					new SSLightSpawnerExample.ColorPreset("Yellow", Color.yellow),
					new SSLightSpawnerExample.ColorPreset("Cyan", Color.cyan),
					new SSLightSpawnerExample.ColorPreset("Magenta", Color.magenta)
				};
			}
			if (SSLightSpawnerExample._shadowType == null)
			{
				SSLightSpawnerExample._shadowType = EnumUtils<LightShadows>.Values;
			}
			SSLightSpawnerExample._shadowStrength = 0f;
			if (SSLightSpawnerExample._lightType == null)
			{
				SSLightSpawnerExample._lightType = EnumUtils<LightType>.Values;
			}
			if (SSLightSpawnerExample._lightShape == null)
			{
				SSLightSpawnerExample._lightShape = EnumUtils<LightShape>.Values;
			}
			SSLightSpawnerExample._spotAngle = 30f;
			SSLightSpawnerExample._innerSpotAngle = 0f;
			this.GenerateNewSettings();
			this.ResendSettings();
			ServerSpecificSettingsSync.SendOnJoinFilter = (ReferenceHub _) => false;
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += this.ProcessUserInput;
		}

		public override void Deactivate()
		{
			ServerSpecificSettingsSync.SendOnJoinFilter = null;
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= this.ProcessUserInput;
		}

		private void GenerateNewSettings()
		{
			List<ServerSpecificSettingBase> list = new List<ServerSpecificSettingBase>();
			list.Add(new SSGroupHeader("Light Spawner", false, null));
			list.Add(new SSSliderSetting(new int?(0), "Intensity", 0f, 100f, SSLightSpawnerExample._lightIntensity, false, "0.00", "x{0}", null));
			list.Add(new SSSliderSetting(new int?(1), "Range", 0f, 100f, SSLightSpawnerExample._lightRange, false, "0.00", "x{0}", null));
			list.Add(new SSDropdownSetting(new int?(2), "Color (preset)", SSLightSpawnerExample._colorPresets.Select((SSLightSpawnerExample.ColorPreset x) => x.Name).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSPlaintextSetting(new int?(3), "Custom Color (R G B)", "...", 10, TMP_InputField.ContentType.Standard, "Leave empty to use a preset."));
			list.Add(SSLightSpawnerExample._selectedColorTextArea = new SSTextArea(new int?(4), "Selected color: None", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft));
			list.Add(new SSDropdownSetting(new int?(5), "Shadow Type", SSLightSpawnerExample._shadowType.Select((LightShadows x) => x.ToString()).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSSliderSetting(new int?(6), "Shadow Strength", 0f, 100f, SSLightSpawnerExample._shadowStrength, false, "0.00", "x{0}", null));
			list.Add(new SSDropdownSetting(new int?(7), "Light Type", SSLightSpawnerExample._lightType.Select((LightType x) => x.ToString()).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSDropdownSetting(new int?(8), "Light Shape", SSLightSpawnerExample._lightShape.Select((LightShape x) => x.ToString()).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSSliderSetting(new int?(9), "Spot Angle", 0f, 100f, SSLightSpawnerExample._spotAngle, false, "0.00", "x{0}", null));
			list.Add(new SSSliderSetting(new int?(10), "Inner Spot Angle", 0f, 100f, SSLightSpawnerExample._innerSpotAngle, false, "0.00", "x{0}", null));
			list.Add(new SSButton(new int?(11), "Confirm Spawning", "Spawn", null, null));
			SSLightSpawnerExample._allSettings = list;
		}

		private bool ValidateUser(ReferenceHub user)
		{
			return PermissionsHandler.IsPermitted(user.serverRoles.Permissions, PlayerPermissions.FacilityManagement);
		}

		private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
		{
			if (!this.ValidateUser(sender))
			{
				return;
			}
			SSButton ssbutton = setting as SSButton;
			if (ssbutton != null && this.TryDestroy(ssbutton.SettingId))
			{
				return;
			}
			SSLightSpawnerExample.ExampleId settingId = (SSLightSpawnerExample.ExampleId)setting.SettingId;
			if (settingId == SSLightSpawnerExample.ExampleId.ColorPresetDropdown)
			{
				SSLightSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(sender), true, null);
				return;
			}
			if (settingId == SSLightSpawnerExample.ExampleId.ConfirmButton)
			{
				this.SpawnLight(sender);
				return;
			}
			if (settingId != SSLightSpawnerExample.ExampleId.DestroyAllButton)
			{
				return;
			}
			this.DestroyAll();
		}

		private void SpawnLight(ReferenceHub sender)
		{
			LightSourceToy lightSourceToy = null;
			using (Dictionary<uint, GameObject>.ValueCollection.Enumerator enumerator = NetworkClient.prefabs.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					LightSourceToy lightSourceToy2;
					if (enumerator.Current.TryGetComponent<LightSourceToy>(out lightSourceToy2))
					{
						lightSourceToy = global::UnityEngine.Object.Instantiate<LightSourceToy>(lightSourceToy2);
						lightSourceToy.OnSpawned(sender, new ArraySegment<string>(new string[0]));
						break;
					}
				}
			}
			if (lightSourceToy == null)
			{
				return;
			}
			lightSourceToy.NetworkLightIntensity = this.GetSettingOfUser<SSSliderSetting>(sender, SSLightSpawnerExample.ExampleId.IntensitySlider).SyncFloatValue;
			lightSourceToy.NetworkLightRange = this.GetSettingOfUser<SSSliderSetting>(sender, SSLightSpawnerExample.ExampleId.RangeSlider).SyncFloatValue;
			Color colorForUser = this.GetColorForUser(sender);
			lightSourceToy.NetworkLightColor = colorForUser;
			int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(sender, SSLightSpawnerExample.ExampleId.ShadowType).SyncSelectionIndexValidated;
			lightSourceToy.NetworkShadowType = (LightShadows)syncSelectionIndexValidated;
			lightSourceToy.NetworkShadowStrength = this.GetSettingOfUser<SSSliderSetting>(sender, SSLightSpawnerExample.ExampleId.ShadowStrength).SyncFloatValue;
			int syncSelectionIndexValidated2 = this.GetSettingOfUser<SSDropdownSetting>(sender, SSLightSpawnerExample.ExampleId.LightType).SyncSelectionIndexValidated;
			lightSourceToy.NetworkLightType = (LightType)syncSelectionIndexValidated2;
			int syncSelectionIndexValidated3 = this.GetSettingOfUser<SSDropdownSetting>(sender, SSLightSpawnerExample.ExampleId.LightShape).SyncSelectionIndexValidated;
			lightSourceToy.NetworkLightShape = (LightShape)syncSelectionIndexValidated3;
			lightSourceToy.NetworkSpotAngle = this.GetSettingOfUser<SSSliderSetting>(sender, SSLightSpawnerExample.ExampleId.SpotAngle).SyncFloatValue;
			lightSourceToy.NetworkInnerSpotAngle = this.GetSettingOfUser<SSSliderSetting>(sender, SSLightSpawnerExample.ExampleId.InnerSpotAngle).SyncFloatValue;
			if (!SSLightSpawnerExample._anySpawned)
			{
				SSLightSpawnerExample._allSettings.Add(new SSGroupHeader("Destroy Spawned Lights", false, null));
				SSLightSpawnerExample._allSettings.Add(new SSButton(new int?(12), "All Lights", "Destroy All (HOLD)", new float?(2f), null));
				SSLightSpawnerExample._anySpawned = true;
			}
			string text = string.Format("{0} Color: {1} SpawnPosition: {2}", lightSourceToy.LightType, colorForUser, lightSourceToy.transform.position);
			string text2 = "Spawned by " + sender.LoggedNameFromRefHub() + " at round time " + RoundStart.RoundLength.ToString("hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
			string text3 = text + "\n" + text2;
			SSLightSpawnerExample._allSettings.Add(new SSButton(new int?((int)lightSourceToy.netId), string.Format("Light NetID#{0}", lightSourceToy.netId), "Destroy (HOLD)", new float?(0.4f), text3));
			this.ResendSettings();
		}

		private void ResendSettings()
		{
			ServerSpecificSettingsSync.DefinedSettings = SSLightSpawnerExample._allSettings.ToArray();
			ServerSpecificSettingsSync.SendToPlayersConditionally(new Func<ReferenceHub, bool>(this.ValidateUser));
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (this.ValidateUser(referenceHub))
				{
					SSLightSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(referenceHub), true, null);
				}
			}
		}

		private void DestroyAll()
		{
			if (!SSLightSpawnerExample._anySpawned)
			{
				return;
			}
			int num = SSLightSpawnerExample._allSettings.Count - 1;
			ServerSpecificSettingBase serverSpecificSettingBase;
			while (num > 0 && SSLightSpawnerExample._allSettings.TryGet(num, out serverSpecificSettingBase) && serverSpecificSettingBase is SSButton)
			{
				this.TryDestroy(serverSpecificSettingBase.SettingId);
				num--;
			}
		}

		private bool TryDestroy(int buttonId)
		{
			NetworkIdentity networkIdentity;
			if (!NetworkUtils.SpawnedNetIds.TryGetValue((uint)buttonId, out networkIdentity))
			{
				return false;
			}
			LightSourceToy lightSourceToy;
			if (!networkIdentity.TryGetComponent<LightSourceToy>(out lightSourceToy))
			{
				return false;
			}
			for (int i = 0; i < SSLightSpawnerExample._allSettings.Count; i++)
			{
				ServerSpecificSettingBase serverSpecificSettingBase = SSLightSpawnerExample._allSettings[i];
				if (serverSpecificSettingBase is SSButton && serverSpecificSettingBase.SettingId == buttonId)
				{
					SSLightSpawnerExample._allSettings.RemoveAt(i);
					break;
				}
			}
			List<ServerSpecificSettingBase> allSettings = SSLightSpawnerExample._allSettings;
			int num = allSettings.Count - 1;
			ServerSpecificSettingBase serverSpecificSettingBase2 = allSettings[num];
			if (serverSpecificSettingBase2 is SSButton && serverSpecificSettingBase2.SettingId == 12)
			{
				SSLightSpawnerExample._anySpawned = false;
				this.GenerateNewSettings();
			}
			NetworkServer.Destroy(lightSourceToy.gameObject);
			this.ResendSettings();
			return true;
		}

		private T GetSettingOfUser<T>(ReferenceHub user, SSLightSpawnerExample.ExampleId id) where T : ServerSpecificSettingBase
		{
			return ServerSpecificSettingsSync.GetSettingOfUser<T>(user, (int)id);
		}

		private string GetColorInfoForUser(ReferenceHub hub)
		{
			Color colorForUser = this.GetColorForUser(hub);
			return "Selected color: <color=" + colorForUser.ToHex() + ">███████████</color>";
		}

		private Color GetColorForUser(ReferenceHub user)
		{
			string[] array = this.GetSettingOfUser<SSPlaintextSetting>(user, SSLightSpawnerExample.ExampleId.CustomColor).SyncInputText.Split(' ', StringSplitOptions.None);
			int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(user, SSLightSpawnerExample.ExampleId.ColorPresetDropdown).SyncSelectionIndexValidated;
			Color color = SSLightSpawnerExample._colorPresets[syncSelectionIndexValidated].Color;
			string text;
			float num2;
			float num = ((array.TryGet(0, out text) && float.TryParse(text, out num2)) ? (num2 / 255f) : color.r);
			string text2;
			float num4;
			float num3 = ((array.TryGet(1, out text2) && float.TryParse(text2, out num4)) ? (num4 / 255f) : color.g);
			string text3;
			float num6;
			float num5 = ((array.TryGet(2, out text3) && float.TryParse(text3, out num6)) ? (num6 / 255f) : color.b);
			return new Color(num, num3, num5);
		}

		private static float _lightIntensity;

		private static float _lightRange;

		private static SSLightSpawnerExample.ColorPreset[] _colorPresets;

		private static LightShadows[] _shadowType;

		private static float _shadowStrength;

		private static LightType[] _lightType;

		private static LightShape[] _lightShape;

		private static float _spotAngle;

		private static float _innerSpotAngle;

		private static List<ServerSpecificSettingBase> _allSettings;

		private static bool _anySpawned;

		private static SSTextArea _selectedColorTextArea;

		private const PlayerPermissions RequiredPermission = PlayerPermissions.FacilityManagement;

		private enum ExampleId
		{
			IntensitySlider,
			RangeSlider,
			ColorPresetDropdown,
			CustomColor,
			ColorInfo,
			ShadowType,
			ShadowStrength,
			LightType,
			LightShape,
			SpotAngle,
			InnerSpotAngle,
			ConfirmButton,
			DestroyAllButton
		}

		private readonly struct ColorPreset
		{
			public ColorPreset(string name, Color color)
			{
				this.Name = name;
				this.Color = color;
			}

			public readonly string Name;

			public readonly Color Color;
		}
	}
}

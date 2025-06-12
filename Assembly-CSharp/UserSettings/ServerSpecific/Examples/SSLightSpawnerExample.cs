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

namespace UserSettings.ServerSpecific.Examples;

public class SSLightSpawnerExample : SSExampleImplementationBase
{
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
		public readonly string Name;

		public readonly Color Color;

		public ColorPreset(string name, Color color)
		{
			this.Name = name;
			this.Color = color;
		}
	}

	private static float _lightIntensity;

	private static float _lightRange;

	private static ColorPreset[] _colorPresets;

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

	public override string Name => "Light Spawner";

	public override void Activate()
	{
		SSLightSpawnerExample._anySpawned = false;
		SSLightSpawnerExample._lightIntensity = 1f;
		SSLightSpawnerExample._lightRange = 10f;
		if (SSLightSpawnerExample._colorPresets == null)
		{
			SSLightSpawnerExample._colorPresets = new ColorPreset[9]
			{
				new ColorPreset("White", Color.white),
				new ColorPreset("Black", Color.black),
				new ColorPreset("Gray", Color.gray),
				new ColorPreset("Red", Color.red),
				new ColorPreset("Green", Color.green),
				new ColorPreset("Blue", Color.blue),
				new ColorPreset("Yellow", Color.yellow),
				new ColorPreset("Cyan", Color.cyan),
				new ColorPreset("Magenta", Color.magenta)
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
		ServerSpecificSettingsSync.ServerOnSettingValueReceived += ProcessUserInput;
	}

	public override void Deactivate()
	{
		ServerSpecificSettingsSync.SendOnJoinFilter = null;
		ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ProcessUserInput;
	}

	private void GenerateNewSettings()
	{
		SSLightSpawnerExample._allSettings = new List<ServerSpecificSettingBase>
		{
			new SSGroupHeader("Light Spawner"),
			new SSSliderSetting(0, "Intensity", 0f, 100f, SSLightSpawnerExample._lightIntensity, integer: false, "0.00", "x{0}"),
			new SSSliderSetting(1, "Range", 0f, 100f, SSLightSpawnerExample._lightRange, integer: false, "0.00", "x{0}"),
			new SSDropdownSetting(2, "Color (preset)", SSLightSpawnerExample._colorPresets.Select((ColorPreset x) => x.Name).ToArray()),
			new SSPlaintextSetting(3, "Custom Color (R G B)", "...", 10, TMP_InputField.ContentType.Standard, "Leave empty to use a preset."),
			SSLightSpawnerExample._selectedColorTextArea = new SSTextArea(4, "Selected color: None"),
			new SSDropdownSetting(5, "Shadow Type", SSLightSpawnerExample._shadowType.Select((LightShadows x) => x.ToString()).ToArray()),
			new SSSliderSetting(6, "Shadow Strength", 0f, 100f, SSLightSpawnerExample._shadowStrength, integer: false, "0.00", "x{0}"),
			new SSDropdownSetting(7, "Light Type", SSLightSpawnerExample._lightType.Select((LightType x) => x.ToString()).ToArray()),
			new SSDropdownSetting(8, "Light Shape", SSLightSpawnerExample._lightShape.Select((LightShape x) => x.ToString()).ToArray()),
			new SSSliderSetting(9, "Spot Angle", 0f, 100f, SSLightSpawnerExample._spotAngle, integer: false, "0.00", "x{0}"),
			new SSSliderSetting(10, "Inner Spot Angle", 0f, 100f, SSLightSpawnerExample._innerSpotAngle, integer: false, "0.00", "x{0}"),
			new SSButton(11, "Confirm Spawning", "Spawn")
		};
	}

	private bool ValidateUser(ReferenceHub user)
	{
		return PermissionsHandler.IsPermitted(user.serverRoles.Permissions, PlayerPermissions.FacilityManagement);
	}

	private void ProcessUserInput(ReferenceHub sender, ServerSpecificSettingBase setting)
	{
		if (this.ValidateUser(sender) && (!(setting is SSButton sSButton) || !this.TryDestroy(sSButton.SettingId)))
		{
			switch ((ExampleId)setting.SettingId)
			{
			case ExampleId.ColorPresetDropdown:
				SSLightSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(sender));
				break;
			case ExampleId.DestroyAllButton:
				this.DestroyAll();
				break;
			case ExampleId.ConfirmButton:
				this.SpawnLight(sender);
				break;
			}
		}
	}

	private void SpawnLight(ReferenceHub sender)
	{
		LightSourceToy lightSourceToy = null;
		foreach (GameObject value in NetworkClient.prefabs.Values)
		{
			if (value.TryGetComponent<LightSourceToy>(out var component))
			{
				lightSourceToy = UnityEngine.Object.Instantiate(component);
				lightSourceToy.OnSpawned(sender, new ArraySegment<string>(new string[0]));
				break;
			}
		}
		if (!(lightSourceToy == null))
		{
			lightSourceToy.NetworkLightIntensity = this.GetSettingOfUser<SSSliderSetting>(sender, ExampleId.IntensitySlider).SyncFloatValue;
			lightSourceToy.NetworkLightRange = this.GetSettingOfUser<SSSliderSetting>(sender, ExampleId.RangeSlider).SyncFloatValue;
			Color color = (lightSourceToy.NetworkLightColor = this.GetColorForUser(sender));
			int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(sender, ExampleId.ShadowType).SyncSelectionIndexValidated;
			lightSourceToy.NetworkShadowType = (LightShadows)syncSelectionIndexValidated;
			lightSourceToy.NetworkShadowStrength = this.GetSettingOfUser<SSSliderSetting>(sender, ExampleId.ShadowStrength).SyncFloatValue;
			int syncSelectionIndexValidated2 = this.GetSettingOfUser<SSDropdownSetting>(sender, ExampleId.LightType).SyncSelectionIndexValidated;
			lightSourceToy.NetworkLightType = (LightType)syncSelectionIndexValidated2;
			int syncSelectionIndexValidated3 = this.GetSettingOfUser<SSDropdownSetting>(sender, ExampleId.LightShape).SyncSelectionIndexValidated;
			lightSourceToy.NetworkLightShape = (LightShape)syncSelectionIndexValidated3;
			lightSourceToy.NetworkSpotAngle = this.GetSettingOfUser<SSSliderSetting>(sender, ExampleId.SpotAngle).SyncFloatValue;
			lightSourceToy.NetworkInnerSpotAngle = this.GetSettingOfUser<SSSliderSetting>(sender, ExampleId.InnerSpotAngle).SyncFloatValue;
			if (!SSLightSpawnerExample._anySpawned)
			{
				SSLightSpawnerExample._allSettings.Add(new SSGroupHeader("Destroy Spawned Lights"));
				SSLightSpawnerExample._allSettings.Add(new SSButton(12, "All Lights", "Destroy All (HOLD)", 2f));
				SSLightSpawnerExample._anySpawned = true;
			}
			string text = $"{lightSourceToy.LightType} Color: {color} SpawnPosition: {lightSourceToy.transform.position}";
			string text2 = "Spawned by " + sender.LoggedNameFromRefHub() + " at round time " + RoundStart.RoundLength.ToString("hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
			string hint = text + "\n" + text2;
			SSLightSpawnerExample._allSettings.Add(new SSButton((int)lightSourceToy.netId, $"Light NetID#{lightSourceToy.netId}", "Destroy (HOLD)", 0.4f, hint));
			this.ResendSettings();
		}
	}

	private void ResendSettings()
	{
		ServerSpecificSettingsSync.DefinedSettings = SSLightSpawnerExample._allSettings.ToArray();
		ServerSpecificSettingsSync.SendToPlayersConditionally(ValidateUser);
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (this.ValidateUser(allHub))
			{
				SSLightSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(allHub));
			}
		}
	}

	private void DestroyAll()
	{
		if (SSLightSpawnerExample._anySpawned)
		{
			int num = SSLightSpawnerExample._allSettings.Count - 1;
			ServerSpecificSettingBase element;
			while (num > 0 && SSLightSpawnerExample._allSettings.TryGet(num, out element) && element is SSButton)
			{
				this.TryDestroy(element.SettingId);
				num--;
			}
		}
	}

	private bool TryDestroy(int buttonId)
	{
		if (!NetworkUtils.SpawnedNetIds.TryGetValue((uint)buttonId, out var value))
		{
			return false;
		}
		if (!value.TryGetComponent<LightSourceToy>(out var component))
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
		ServerSpecificSettingBase serverSpecificSettingBase2 = allSettings[allSettings.Count - 1];
		if (serverSpecificSettingBase2 is SSButton && serverSpecificSettingBase2.SettingId == 12)
		{
			SSLightSpawnerExample._anySpawned = false;
			this.GenerateNewSettings();
		}
		NetworkServer.Destroy(component.gameObject);
		this.ResendSettings();
		return true;
	}

	private T GetSettingOfUser<T>(ReferenceHub user, ExampleId id) where T : ServerSpecificSettingBase
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
		string[] array = this.GetSettingOfUser<SSPlaintextSetting>(user, ExampleId.CustomColor).SyncInputText.Split(' ');
		int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(user, ExampleId.ColorPresetDropdown).SyncSelectionIndexValidated;
		Color color = SSLightSpawnerExample._colorPresets[syncSelectionIndexValidated].Color;
		string element;
		float result;
		float r = ((array.TryGet(0, out element) && float.TryParse(element, out result)) ? (result / 255f) : color.r);
		string element2;
		float result2;
		float g = ((array.TryGet(1, out element2) && float.TryParse(element2, out result2)) ? (result2 / 255f) : color.g);
		string element3;
		float result3;
		float b = ((array.TryGet(2, out element3) && float.TryParse(element3, out result3)) ? (result3 / 255f) : color.b);
		return new Color(r, g, b);
	}
}

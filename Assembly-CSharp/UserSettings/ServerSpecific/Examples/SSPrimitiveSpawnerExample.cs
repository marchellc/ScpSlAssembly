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
	public class SSPrimitiveSpawnerExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "Primitive Spawner";
			}
		}

		public override void Activate()
		{
			SSPrimitiveSpawnerExample._anySpawned = false;
			if (SSPrimitiveSpawnerExample._colorPresets == null)
			{
				SSPrimitiveSpawnerExample._colorPresets = new SSPrimitiveSpawnerExample.ColorPreset[]
				{
					new SSPrimitiveSpawnerExample.ColorPreset("White", Color.white),
					new SSPrimitiveSpawnerExample.ColorPreset("Black", Color.black),
					new SSPrimitiveSpawnerExample.ColorPreset("Gray", Color.gray),
					new SSPrimitiveSpawnerExample.ColorPreset("Red", Color.red),
					new SSPrimitiveSpawnerExample.ColorPreset("Green", Color.green),
					new SSPrimitiveSpawnerExample.ColorPreset("Blue", Color.blue),
					new SSPrimitiveSpawnerExample.ColorPreset("Yellow", Color.yellow),
					new SSPrimitiveSpawnerExample.ColorPreset("Cyan", Color.cyan),
					new SSPrimitiveSpawnerExample.ColorPreset("Magenta", Color.magenta)
				};
			}
			if (SSPrimitiveSpawnerExample._primitiveTypes == null)
			{
				SSPrimitiveSpawnerExample._primitiveTypes = EnumUtils<PrimitiveType>.Values;
			}
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
			list.Add(new SSGroupHeader("Primitive Spawner", false, null));
			list.Add(new SSDropdownSetting(new int?(2), "Type", SSPrimitiveSpawnerExample._primitiveTypes.Select((PrimitiveType x) => x.ToString()).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSDropdownSetting(new int?(3), "Color (preset)", SSPrimitiveSpawnerExample._colorPresets.Select((SSPrimitiveSpawnerExample.ColorPreset x) => x.Name).ToArray<string>(), 0, SSDropdownSetting.DropdownEntryType.Regular, null));
			list.Add(new SSSliderSetting(new int?(5), "Opacity", 0f, 100f, 100f, true, "0.##", "{0}%", null));
			list.Add(new SSPlaintextSetting(new int?(4), "Custom Color (R G B)", "...", 10, TMP_InputField.ContentType.Standard, "Leave empty to use a preset."));
			list.Add(SSPrimitiveSpawnerExample._selectedColorTextArea = new SSTextArea(new int?(8), "Selected color: None", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft));
			list.Add(new SSTwoButtonsSetting(new int?(6), "Collisions", "Disabled", "Enabled", true, null));
			list.Add(new SSTwoButtonsSetting(new int?(7), "Renderer", "Invisible", "Visible", true, "Invisible primitives can still receive collisions."));
			list.Add(new SSSliderSetting(new int?(9), "Scale (X)", 0f, 25f, 1f, false, "0.00", "x{0}", null));
			list.Add(new SSSliderSetting(new int?(10), "Scale (Y)", 0f, 25f, 1f, false, "0.00", "x{0}", null));
			list.Add(new SSSliderSetting(new int?(11), "Scale (Z)", 0f, 25f, 1f, false, "0.00", "x{0}", null));
			list.Add(new SSButton(new int?(0), "Confirm Spawning", "Spawn", null, null));
			SSPrimitiveSpawnerExample._allSettings = list;
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
			switch (setting.SettingId)
			{
			case 0:
				this.SpawnPrimitive(sender);
				break;
			case 1:
				this.DestroyAll();
				return;
			case 2:
				break;
			case 3:
			case 4:
			case 5:
				SSPrimitiveSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(sender), true, null);
				return;
			default:
				return;
			}
		}

		private void SpawnPrimitive(ReferenceHub sender)
		{
			PrimitiveObjectToy primitiveObjectToy = null;
			using (Dictionary<uint, GameObject>.ValueCollection.Enumerator enumerator = NetworkClient.prefabs.Values.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PrimitiveObjectToy primitiveObjectToy2;
					if (enumerator.Current.TryGetComponent<PrimitiveObjectToy>(out primitiveObjectToy2))
					{
						primitiveObjectToy = global::UnityEngine.Object.Instantiate<PrimitiveObjectToy>(primitiveObjectToy2);
						primitiveObjectToy.OnSpawned(sender, new ArraySegment<string>(new string[0]));
						break;
					}
				}
			}
			if (primitiveObjectToy == null)
			{
				return;
			}
			int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.TypeDropdown).SyncSelectionIndexValidated;
			primitiveObjectToy.NetworkPrimitiveType = (PrimitiveType)syncSelectionIndexValidated;
			Color colorForUser = this.GetColorForUser(sender);
			primitiveObjectToy.NetworkMaterialColor = colorForUser;
			float syncFloatValue = this.GetSettingOfUser<SSSliderSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.ScaleSliderX).SyncFloatValue;
			float syncFloatValue2 = this.GetSettingOfUser<SSSliderSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.ScaleSliderY).SyncFloatValue;
			float syncFloatValue3 = this.GetSettingOfUser<SSSliderSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.ScaleSliderZ).SyncFloatValue;
			Vector3 vector = new Vector3(syncFloatValue, syncFloatValue2, syncFloatValue3);
			primitiveObjectToy.transform.localScale = vector;
			primitiveObjectToy.NetworkScale = vector;
			PrimitiveFlags primitiveFlags = (this.GetSettingOfUser<SSTwoButtonsSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.CollisionsToggle).SyncIsB ? PrimitiveFlags.Collidable : PrimitiveFlags.None);
			PrimitiveFlags primitiveFlags2 = (this.GetSettingOfUser<SSTwoButtonsSetting>(sender, SSPrimitiveSpawnerExample.ExampleId.RendererToggle).SyncIsB ? PrimitiveFlags.Visible : PrimitiveFlags.None);
			primitiveObjectToy.NetworkPrimitiveFlags = primitiveFlags | primitiveFlags2;
			if (!SSPrimitiveSpawnerExample._anySpawned)
			{
				SSPrimitiveSpawnerExample._allSettings.Add(new SSGroupHeader("Destroy Spawned Primitives", false, null));
				SSPrimitiveSpawnerExample._allSettings.Add(new SSButton(new int?(1), "All Primitives", "Destroy All (HOLD)", new float?(2f), null));
				SSPrimitiveSpawnerExample._anySpawned = true;
			}
			string text = string.Format("{0} Color: {1} Size: {2} SpawnPosition: {3}", new object[]
			{
				primitiveObjectToy.PrimitiveType,
				colorForUser,
				vector,
				primitiveObjectToy.transform.position
			});
			string text2 = "Spawned by " + sender.LoggedNameFromRefHub() + " at round time " + RoundStart.RoundLength.ToString("hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
			string text3 = text + "\n" + text2;
			SSPrimitiveSpawnerExample._allSettings.Add(new SSButton(new int?((int)primitiveObjectToy.netId), string.Format("Primitive NetID#{0}", primitiveObjectToy.netId), "Destroy (HOLD)", new float?(0.4f), text3));
			this.ResendSettings();
		}

		private void ResendSettings()
		{
			ServerSpecificSettingsSync.DefinedSettings = SSPrimitiveSpawnerExample._allSettings.ToArray();
			ServerSpecificSettingsSync.SendToPlayersConditionally(new Func<ReferenceHub, bool>(this.ValidateUser));
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				if (this.ValidateUser(referenceHub))
				{
					SSPrimitiveSpawnerExample._selectedColorTextArea.SendTextUpdate(this.GetColorInfoForUser(referenceHub), true, null);
				}
			}
		}

		private void DestroyAll()
		{
			if (!SSPrimitiveSpawnerExample._anySpawned)
			{
				return;
			}
			int num = SSPrimitiveSpawnerExample._allSettings.Count - 1;
			ServerSpecificSettingBase serverSpecificSettingBase;
			while (num > 0 && SSPrimitiveSpawnerExample._allSettings.TryGet(num, out serverSpecificSettingBase) && serverSpecificSettingBase is SSButton)
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
			PrimitiveObjectToy primitiveObjectToy;
			if (!networkIdentity.TryGetComponent<PrimitiveObjectToy>(out primitiveObjectToy))
			{
				return false;
			}
			for (int i = 0; i < SSPrimitiveSpawnerExample._allSettings.Count; i++)
			{
				ServerSpecificSettingBase serverSpecificSettingBase = SSPrimitiveSpawnerExample._allSettings[i];
				if (serverSpecificSettingBase is SSButton && serverSpecificSettingBase.SettingId == buttonId)
				{
					SSPrimitiveSpawnerExample._allSettings.RemoveAt(i);
					break;
				}
			}
			List<ServerSpecificSettingBase> allSettings = SSPrimitiveSpawnerExample._allSettings;
			int num = allSettings.Count - 1;
			ServerSpecificSettingBase serverSpecificSettingBase2 = allSettings[num];
			if (serverSpecificSettingBase2 is SSButton && serverSpecificSettingBase2.SettingId == 1)
			{
				SSPrimitiveSpawnerExample._anySpawned = false;
				this.GenerateNewSettings();
			}
			NetworkServer.Destroy(primitiveObjectToy.gameObject);
			this.ResendSettings();
			return true;
		}

		private T GetSettingOfUser<T>(ReferenceHub user, SSPrimitiveSpawnerExample.ExampleId id) where T : ServerSpecificSettingBase
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
			string[] array = this.GetSettingOfUser<SSPlaintextSetting>(user, SSPrimitiveSpawnerExample.ExampleId.ColorField).SyncInputText.Split(' ', StringSplitOptions.None);
			int syncSelectionIndexValidated = this.GetSettingOfUser<SSDropdownSetting>(user, SSPrimitiveSpawnerExample.ExampleId.ColorPresetDropdown).SyncSelectionIndexValidated;
			Color color = SSPrimitiveSpawnerExample._colorPresets[syncSelectionIndexValidated].Color;
			string text;
			float num2;
			float num = ((array.TryGet(0, out text) && float.TryParse(text, out num2)) ? (num2 / 255f) : color.r);
			string text2;
			float num4;
			float num3 = ((array.TryGet(1, out text2) && float.TryParse(text2, out num4)) ? (num4 / 255f) : color.g);
			string text3;
			float num6;
			float num5 = ((array.TryGet(2, out text3) && float.TryParse(text3, out num6)) ? (num6 / 255f) : color.b);
			float num7 = this.GetSettingOfUser<SSSliderSetting>(user, SSPrimitiveSpawnerExample.ExampleId.ColorAlphaSlider).SyncFloatValue / 100f;
			return new Color(num, num3, num5, num7);
		}

		private static PrimitiveType[] _primitiveTypes;

		private static SSPrimitiveSpawnerExample.ColorPreset[] _colorPresets;

		private static List<ServerSpecificSettingBase> _allSettings;

		private static bool _anySpawned;

		private static SSTextArea _selectedColorTextArea;

		private const PlayerPermissions RequiredPermission = PlayerPermissions.FacilityManagement;

		private enum ExampleId
		{
			ConfirmButton,
			DestroyAllButton,
			TypeDropdown,
			ColorPresetDropdown,
			ColorField,
			ColorAlphaSlider,
			CollisionsToggle,
			RendererToggle,
			ColorInfo,
			ScaleSliderX,
			ScaleSliderY,
			ScaleSliderZ
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

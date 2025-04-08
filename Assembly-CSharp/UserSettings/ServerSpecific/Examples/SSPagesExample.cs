using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UserSettings.ServerSpecific.Examples
{
	public class SSPagesExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "Multiple pages demo";
			}
		}

		public override void Activate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += this.ServerOnSettingValueReceived;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Combine(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerDisconnected));
			this._lastSentPages = new Dictionary<ReferenceHub, int>();
			this._pages = new SSPagesExample.SettingsPage[]
			{
				new SSPagesExample.SettingsPage("Page A", new ServerSpecificSettingBase[]
				{
					new SSKeybindSetting(null, "Keybind at Page A", KeyCode.None, true, null),
					new SSPlaintextSetting(null, "Plaintext Input Field at Page A", "...", 64, TMP_InputField.ContentType.Standard, null),
					new SSTextArea(null, "Just a generic text area for page A!", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
					new SSSliderSetting(null, "Example slider at page A", 0f, 1f, 0f, false, "0.##", "{0}", null)
				}),
				new SSPagesExample.SettingsPage("Page B", new ServerSpecificSettingBase[]
				{
					new SSTwoButtonsSetting(null, "Which page is your favorite?", "Page B", "Also Page B", false, null),
					new SSDropdownSetting(null, "Please rate this page", new string[] { "10/10", "5/5", "B", "★★★★★" }, 0, SSDropdownSetting.DropdownEntryType.Regular, null),
					new SSButton(null, "\"B\", as in \"Button\"", "BBBB", null, null),
					new SSTextArea(null, "Page B stands for <color=red><b><i>BESTEST PAGE</i></b></color>", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft)
				}),
				new SSPagesExample.SettingsPage("Page C", new ServerSpecificSettingBase[]
				{
					new SSSliderSetting(null, "Slider C1", 0f, 1f, 0f, false, "0.##", "{0}", null),
					new SSSliderSetting(null, "Slider C2", 0f, 1f, 0f, false, "0.##", "{0}", null),
					new SSSliderSetting(null, "Slider C3", 0f, 1f, 0f, false, "0.##", "{0}", null),
					new SSTwoButtonsSetting(null, "Two buttons", "C1", "C2", false, null),
					new SSGroupHeader("Subcategory", true, "You can still make additional subcategories using group headers."),
					new SSDropdownSetting(null, "Dropdown C", new string[] { "C1", "C2", "C3" }, 0, SSDropdownSetting.DropdownEntryType.Scrollable, null)
				})
			};
			string[] array = new string[this._pages.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = string.Format("{0} ({1} out of {2})", this._pages[i].Name, i + 1, this._pages.Length);
			}
			this._pinnedSection = new ServerSpecificSettingBase[]
			{
				this._pageSelectorDropdown = new SSDropdownSetting(null, "Page", array, 0, SSDropdownSetting.DropdownEntryType.HybridLoop, null),
				new SSButton(null, "Another Pinned Element", "Do Nothing", null, "This button doesn't do anything, but it shows you can \"pin\" multiple elements.")
			};
			this._pages.ForEach(delegate(SSPagesExample.SettingsPage page)
			{
				page.GenerateCombinedEntries(this._pinnedSection);
			});
			List<ServerSpecificSettingBase> allSettings = new List<ServerSpecificSettingBase>(this._pinnedSection);
			this._pages.ForEach(delegate(SSPagesExample.SettingsPage page)
			{
				allSettings.AddRange(page.OwnEntries);
			});
			ServerSpecificSettingsSync.DefinedSettings = allSettings.ToArray();
			ServerSpecificSettingsSync.SendToAll();
		}

		public override void Deactivate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= this.ServerOnSettingValueReceived;
			ReferenceHub.OnPlayerRemoved = (Action<ReferenceHub>)Delegate.Remove(ReferenceHub.OnPlayerRemoved, new Action<ReferenceHub>(this.OnPlayerDisconnected));
		}

		private void ServerOnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
		{
			SSDropdownSetting ssdropdownSetting = setting as SSDropdownSetting;
			if (ssdropdownSetting != null && ssdropdownSetting.SettingId == this._pageSelectorDropdown.SettingId)
			{
				this.ServerSendSettingsPage(hub, ssdropdownSetting.SyncSelectionIndexValidated);
			}
		}

		private void ServerSendSettingsPage(ReferenceHub hub, int settingIndex)
		{
			int num;
			if (this._lastSentPages.TryGetValue(hub, out num) && num == settingIndex)
			{
				return;
			}
			this._lastSentPages[hub] = settingIndex;
			ServerSpecificSettingsSync.SendToPlayer(hub, this._pages[settingIndex].CombinedEntries, null);
		}

		private void OnPlayerDisconnected(ReferenceHub hub)
		{
			Dictionary<ReferenceHub, int> lastSentPages = this._lastSentPages;
			if (lastSentPages == null)
			{
				return;
			}
			lastSentPages.Remove(hub);
		}

		private SSDropdownSetting _pageSelectorDropdown;

		private ServerSpecificSettingBase[] _pinnedSection;

		private SSPagesExample.SettingsPage[] _pages;

		private Dictionary<ReferenceHub, int> _lastSentPages;

		public class SettingsPage
		{
			public ServerSpecificSettingBase[] CombinedEntries { get; private set; }

			public SettingsPage(string name, ServerSpecificSettingBase[] entries)
			{
				this.Name = name;
				this.OwnEntries = entries;
			}

			public void GenerateCombinedEntries(ServerSpecificSettingBase[] pageSelectorSection)
			{
				int num = pageSelectorSection.Length + this.OwnEntries.Length + 1;
				this.CombinedEntries = new ServerSpecificSettingBase[num];
				int num2 = 0;
				foreach (ServerSpecificSettingBase serverSpecificSettingBase in pageSelectorSection)
				{
					this.CombinedEntries[num2++] = serverSpecificSettingBase;
				}
				this.CombinedEntries[num2++] = new SSGroupHeader(this.Name, false, null);
				foreach (ServerSpecificSettingBase serverSpecificSettingBase2 in this.OwnEntries)
				{
					this.CombinedEntries[num2++] = serverSpecificSettingBase2;
				}
			}

			public readonly string Name;

			public readonly ServerSpecificSettingBase[] OwnEntries;
		}
	}
}

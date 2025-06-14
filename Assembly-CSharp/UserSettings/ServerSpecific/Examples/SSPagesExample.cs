using System.Collections.Generic;

namespace UserSettings.ServerSpecific.Examples;

public class SSPagesExample : SSExampleImplementationBase
{
	public class SettingsPage
	{
		public readonly string Name;

		public readonly ServerSpecificSettingBase[] OwnEntries;

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
			ServerSpecificSettingBase[] array = pageSelectorSection;
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in array)
			{
				this.CombinedEntries[num2++] = serverSpecificSettingBase;
			}
			this.CombinedEntries[num2++] = new SSGroupHeader(this.Name);
			array = this.OwnEntries;
			foreach (ServerSpecificSettingBase serverSpecificSettingBase2 in array)
			{
				this.CombinedEntries[num2++] = serverSpecificSettingBase2;
			}
		}
	}

	private SSDropdownSetting _pageSelectorDropdown;

	private ServerSpecificSettingBase[] _pinnedSection;

	private SettingsPage[] _pages;

	private Dictionary<ReferenceHub, int> _lastSentPages;

	public override string Name => "Multiple pages demo";

	public override void Activate()
	{
		ServerSpecificSettingsSync.ServerOnSettingValueReceived += ServerOnSettingValueReceived;
		ReferenceHub.OnPlayerRemoved += OnPlayerDisconnected;
		this._lastSentPages = new Dictionary<ReferenceHub, int>();
		this._pages = new SettingsPage[3]
		{
			new SettingsPage("Page A", new ServerSpecificSettingBase[4]
			{
				new SSKeybindSetting(null, "Keybind at Page A"),
				new SSPlaintextSetting(null, "Plaintext Input Field at Page A"),
				new SSTextArea(null, "Just a generic text area for page A!"),
				new SSSliderSetting(null, "Example slider at page A", 0f, 1f)
			}),
			new SettingsPage("Page B", new ServerSpecificSettingBase[4]
			{
				new SSTwoButtonsSetting(null, "Which page is your favorite?", "Page B", "Also Page B"),
				new SSDropdownSetting(null, "Please rate this page", new string[4] { "10/10", "5/5", "B", "★★★★★" }),
				new SSButton(null, "\"B\", as in \"Button\"", "BBBB"),
				new SSTextArea(null, "Page B stands for <color=red><b><i>BESTEST PAGE</i></b></color>")
			}),
			new SettingsPage("Page C", new ServerSpecificSettingBase[6]
			{
				new SSSliderSetting(null, "Slider C1", 0f, 1f),
				new SSSliderSetting(null, "Slider C2", 0f, 1f),
				new SSSliderSetting(null, "Slider C3", 0f, 1f),
				new SSTwoButtonsSetting(null, "Two buttons", "C1", "C2"),
				new SSGroupHeader("Subcategory", reducedPadding: true, "You can still make additional subcategories using group headers."),
				new SSDropdownSetting(null, "Dropdown C", new string[3] { "C1", "C2", "C3" }, 0, SSDropdownSetting.DropdownEntryType.Scrollable)
			})
		};
		string[] array = new string[this._pages.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = $"{this._pages[i].Name} ({i + 1} out of {this._pages.Length})";
		}
		this._pinnedSection = new ServerSpecificSettingBase[2]
		{
			this._pageSelectorDropdown = new SSDropdownSetting(null, "Page", array, 0, SSDropdownSetting.DropdownEntryType.HybridLoop),
			new SSButton(null, "Another Pinned Element", "Do Nothing", null, "This button doesn't do anything, but it shows you can \"pin\" multiple elements.")
		};
		this._pages.ForEach(delegate(SettingsPage page)
		{
			page.GenerateCombinedEntries(this._pinnedSection);
		});
		List<ServerSpecificSettingBase> allSettings = new List<ServerSpecificSettingBase>(this._pinnedSection);
		this._pages.ForEach(delegate(SettingsPage page)
		{
			allSettings.AddRange(page.OwnEntries);
		});
		ServerSpecificSettingsSync.DefinedSettings = allSettings.ToArray();
		ServerSpecificSettingsSync.SendToAll();
	}

	public override void Deactivate()
	{
		ServerSpecificSettingsSync.ServerOnSettingValueReceived -= ServerOnSettingValueReceived;
		ReferenceHub.OnPlayerRemoved -= OnPlayerDisconnected;
	}

	private void ServerOnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
	{
		if (setting is SSDropdownSetting sSDropdownSetting && sSDropdownSetting.SettingId == this._pageSelectorDropdown.SettingId)
		{
			this.ServerSendSettingsPage(hub, sSDropdownSetting.SyncSelectionIndexValidated);
		}
	}

	private void ServerSendSettingsPage(ReferenceHub hub, int settingIndex)
	{
		if (!this._lastSentPages.TryGetValue(hub, out var value) || value != settingIndex)
		{
			this._lastSentPages[hub] = settingIndex;
			ServerSpecificSettingsSync.SendToPlayer(hub, this._pages[settingIndex].CombinedEntries);
		}
	}

	private void OnPlayerDisconnected(ReferenceHub hub)
	{
		this._lastSentPages?.Remove(hub);
	}
}

using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSEntrySpawner : MonoBehaviour
{
	private static SSEntrySpawner _singleton;

	private static int? _clientVersionCache;

	[SerializeField]
	private GameObject[] _entryTemplates;

	[SerializeField]
	private Transform _entriesParentTr;

	[SerializeField]
	private UserSettingsCategories _categoriesController;

	[SerializeField]
	private GameObject _categoryButton;

	[SerializeField]
	private GameObject _categoryRoot;

	[SerializeField]
	private GameObject _spacer;

	[SerializeField]
	private GameObject[] _newSettingsWarning;

	private ISSEntry[] _cachedComponents;

	private readonly List<GameObject> _spawnedEntries = new List<GameObject>();

	private readonly List<VerticalLayoutGroup> _layoutGroups = new List<VerticalLayoutGroup>();

	private static string PrefsKey => "SrvSp_" + ServerSpecificSettingsSync.CurServerPrefsKey + "_Version";

	private static int ClientVersion
	{
		get
		{
			int valueOrDefault = SSEntrySpawner._clientVersionCache.GetValueOrDefault();
			if (!SSEntrySpawner._clientVersionCache.HasValue)
			{
				valueOrDefault = PlayerPrefs.GetInt(SSEntrySpawner.PrefsKey, 0);
				SSEntrySpawner._clientVersionCache = valueOrDefault;
			}
			return SSEntrySpawner._clientVersionCache.Value;
		}
		set
		{
			if (SSEntrySpawner._clientVersionCache != value)
			{
				SSEntrySpawner._clientVersionCache = value;
				PlayerPrefs.SetInt(SSEntrySpawner.PrefsKey, value);
			}
		}
	}

	private void Awake()
	{
		SSEntrySpawner._singleton = this;
		SSEntrySpawner._clientVersionCache = null;
		SSTabDetector.OnStatusChanged += OnTabChanged;
	}

	private void Update()
	{
		if (!SSTabDetector.IsOpen)
		{
			return;
		}
		foreach (VerticalLayoutGroup layoutGroup in this._layoutGroups)
		{
			layoutGroup.enabled = false;
			layoutGroup.enabled = true;
		}
	}

	private void OnDestroy()
	{
		SSTabDetector.OnStatusChanged -= OnTabChanged;
	}

	private void OnTabChanged()
	{
		if (SSTabDetector.IsOpen)
		{
			this.ToggleWarning(state: false);
			SSEntrySpawner.ClientVersion = ServerSpecificSettingsSync.Version;
		}
		SSEntrySpawner.ClientSendReport();
	}

	private void ToggleWarning(bool state)
	{
		GameObject[] newSettingsWarning = this._newSettingsWarning;
		for (int i = 0; i < newSettingsWarning.Length; i++)
		{
			newSettingsWarning[i].SetActive(state);
		}
	}

	private void DeleteSpawnedEntries()
	{
		this.ToggleWarning(state: false);
		foreach (GameObject spawnedEntry in this._spawnedEntries)
		{
			if (!(spawnedEntry == null))
			{
				UnityEngine.Object.Destroy(spawnedEntry);
			}
		}
		this._spawnedEntries.Clear();
	}

	private void SpawnAllEntries(ServerSpecificSettingBase[] settings, bool showWarning)
	{
		this._spacer.SetActive(!(settings[0] is SSGroupHeader));
		settings.ForEach(SpawnEntry);
		this._categoryButton.SetActive(value: true);
		this._layoutGroups.Clear();
		this._categoryRoot.GetComponentsInChildren(this._layoutGroups);
		this.ToggleWarning(showWarning);
	}

	private void SpawnEntry(ServerSpecificSettingBase setting)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(this.GetTemplateForSetting(setting), this._entriesParentTr);
		gameObject.SetActive(value: true);
		gameObject.GetComponentInChildren<ISSEntry>().Init(setting);
		this._spawnedEntries.Add(gameObject);
	}

	private GameObject GetTemplateForSetting(ServerSpecificSettingBase setting)
	{
		int num = this._entryTemplates.Length;
		if (this._cachedComponents == null)
		{
			this._cachedComponents = new ISSEntry[num];
		}
		for (int i = 0; i < num; i++)
		{
			ISSEntry iSSEntry = this._cachedComponents[i];
			if (iSSEntry == null)
			{
				iSSEntry = this.FindEntryInTemplate(this._entryTemplates[i]);
				this._cachedComponents[i] = iSSEntry;
			}
			if (iSSEntry.CheckCompatibility(setting))
			{
				return this._entryTemplates[i];
			}
		}
		throw new InvalidOperationException("This setting does not have a compatible entry: " + setting);
	}

	private ISSEntry FindEntryInTemplate(GameObject template)
	{
		ISSEntry componentInChildren = template.GetComponentInChildren<ISSEntry>(includeInactive: true);
		if (componentInChildren as UnityEngine.Object == null)
		{
			throw new InvalidOperationException("This entry template is not valid: " + template.name);
		}
		return componentInChildren;
	}

	private static void ClientSendReport()
	{
		NetworkClient.Send(new SSSUserStatusReport(SSEntrySpawner.ClientVersion, SSTabDetector.IsOpen));
	}

	public static void Refresh()
	{
		if (!(SSEntrySpawner._singleton == null))
		{
			SSEntrySpawner._singleton.DeleteSpawnedEntries();
			ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
			if (definedSettings == null || definedSettings.Length == 0)
			{
				SSEntrySpawner._singleton._categoriesController.ResetSelection();
				SSEntrySpawner._singleton._categoryButton.SetActive(value: false);
				return;
			}
			int clientVersion = SSEntrySpawner.ClientVersion;
			int version = ServerSpecificSettingsSync.Version;
			SSEntrySpawner._singleton.SpawnAllEntries(definedSettings, version != 0 && clientVersion != version);
			SSEntrySpawner.ClientSendReport();
		}
	}
}

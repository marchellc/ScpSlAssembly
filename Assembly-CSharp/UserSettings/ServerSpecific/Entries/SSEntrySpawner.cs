using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSEntrySpawner : MonoBehaviour
	{
		private static string PrefsKey
		{
			get
			{
				return "SrvSp_" + ServerSpecificSettingsSync.CurServerPrefsKey + "_Version";
			}
		}

		private static int ClientVersion
		{
			get
			{
				int num = SSEntrySpawner._clientVersionCache.GetValueOrDefault();
				if (SSEntrySpawner._clientVersionCache == null)
				{
					num = PlayerPrefs.GetInt(SSEntrySpawner.PrefsKey, 0);
					SSEntrySpawner._clientVersionCache = new int?(num);
				}
				return SSEntrySpawner._clientVersionCache.Value;
			}
			set
			{
				int? clientVersionCache = SSEntrySpawner._clientVersionCache;
				if ((clientVersionCache.GetValueOrDefault() == value) & (clientVersionCache != null))
				{
					return;
				}
				SSEntrySpawner._clientVersionCache = new int?(value);
				PlayerPrefs.SetInt(SSEntrySpawner.PrefsKey, value);
			}
		}

		private void Awake()
		{
			SSEntrySpawner._singleton = this;
			SSEntrySpawner._clientVersionCache = null;
			SSTabDetector.OnStatusChanged += this.OnTabChanged;
		}

		private void Update()
		{
			if (!SSTabDetector.IsOpen)
			{
				return;
			}
			foreach (VerticalLayoutGroup verticalLayoutGroup in this._layoutGroups)
			{
				verticalLayoutGroup.enabled = false;
				verticalLayoutGroup.enabled = true;
			}
		}

		private void OnDestroy()
		{
			SSTabDetector.OnStatusChanged -= this.OnTabChanged;
		}

		private void OnTabChanged()
		{
			if (SSTabDetector.IsOpen)
			{
				this.ToggleWarning(false);
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
			this.ToggleWarning(false);
			foreach (GameObject gameObject in this._spawnedEntries)
			{
				if (!(gameObject == null))
				{
					global::UnityEngine.Object.Destroy(gameObject);
				}
			}
			this._spawnedEntries.Clear();
		}

		private void SpawnAllEntries(ServerSpecificSettingBase[] settings, bool showWarning)
		{
			this._spacer.SetActive(!(settings[0] is SSGroupHeader));
			settings.ForEach(new Action<ServerSpecificSettingBase>(this.SpawnEntry));
			this._categoryButton.SetActive(true);
			this._layoutGroups.Clear();
			this._categoryRoot.GetComponentsInChildren<VerticalLayoutGroup>(this._layoutGroups);
			this.ToggleWarning(showWarning);
		}

		private void SpawnEntry(ServerSpecificSettingBase setting)
		{
			GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>(this.GetTemplateForSetting(setting), this._entriesParentTr);
			gameObject.SetActive(true);
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
				ISSEntry issentry = this._cachedComponents[i];
				if (issentry == null)
				{
					issentry = this.FindEntryInTemplate(this._entryTemplates[i]);
					this._cachedComponents[i] = issentry;
				}
				if (issentry.CheckCompatibility(setting))
				{
					return this._entryTemplates[i];
				}
			}
			throw new InvalidOperationException("This setting does not have a compatible entry: " + ((setting != null) ? setting.ToString() : null));
		}

		private ISSEntry FindEntryInTemplate(GameObject template)
		{
			ISSEntry componentInChildren = template.GetComponentInChildren<ISSEntry>(true);
			if (componentInChildren as global::UnityEngine.Object == null)
			{
				throw new InvalidOperationException("This entry template is not valid: " + template.name);
			}
			return componentInChildren;
		}

		private static void ClientSendReport()
		{
			NetworkClient.Send<SSSUserStatusReport>(new SSSUserStatusReport(SSEntrySpawner.ClientVersion, SSTabDetector.IsOpen), 0);
		}

		public static void Refresh()
		{
			if (SSEntrySpawner._singleton == null)
			{
				return;
			}
			SSEntrySpawner._singleton.DeleteSpawnedEntries();
			ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
			if (definedSettings == null || definedSettings.Length == 0)
			{
				SSEntrySpawner._singleton._categoriesController.ResetSelection();
				SSEntrySpawner._singleton._categoryButton.SetActive(false);
				return;
			}
			int clientVersion = SSEntrySpawner.ClientVersion;
			int version = ServerSpecificSettingsSync.Version;
			SSEntrySpawner._singleton.SpawnAllEntries(definedSettings, version != 0 && clientVersion != version);
			SSEntrySpawner.ClientSendReport();
		}

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
	}
}

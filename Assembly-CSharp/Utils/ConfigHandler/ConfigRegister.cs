using System;
using System.Collections.Generic;

namespace Utils.ConfigHandler;

public abstract class ConfigRegister
{
	protected readonly List<ConfigEntry> registeredConfigs = new List<ConfigEntry>();

	public ConfigEntry[] GetRegisteredConfigs()
	{
		return this.registeredConfigs.ToArray();
	}

	public ConfigEntry GetRegisteredConfig(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		foreach (ConfigEntry registeredConfig in this.registeredConfigs)
		{
			if (string.Equals(key, registeredConfig.Key, StringComparison.OrdinalIgnoreCase))
			{
				return registeredConfig;
			}
		}
		return null;
	}

	public void RegisterConfig(ConfigEntry configEntry, bool updateValue = true)
	{
		if (configEntry != null && !string.IsNullOrEmpty(configEntry.Key))
		{
			this.registeredConfigs.Add(configEntry);
			if (updateValue)
			{
				this.UpdateConfigValue(configEntry);
			}
		}
	}

	public void RegisterConfigs(ConfigEntry[] configEntries, bool updateValue = true)
	{
		if (configEntries != null)
		{
			foreach (ConfigEntry configEntry in configEntries)
			{
				this.RegisterConfig(configEntry, updateValue);
			}
		}
	}

	public void UnRegisterConfig(ConfigEntry configEntry)
	{
		if (configEntry != null && !string.IsNullOrEmpty(configEntry.Key))
		{
			this.registeredConfigs.Remove(configEntry);
		}
	}

	public void UnRegisterConfig(string key)
	{
		this.UnRegisterConfig(this.GetRegisteredConfig(key));
	}

	public void UnRegisterConfigs(params ConfigEntry[] configEntries)
	{
		if (configEntries != null)
		{
			foreach (ConfigEntry configEntry in configEntries)
			{
				this.UnRegisterConfig(configEntry);
			}
		}
	}

	public void UnRegisterConfigs(params string[] keys)
	{
		if (keys != null)
		{
			foreach (string key in keys)
			{
				this.UnRegisterConfig(key);
			}
		}
	}

	public void UnRegisterConfigs()
	{
		foreach (ConfigEntry registeredConfig in this.registeredConfigs)
		{
			this.UnRegisterConfig(registeredConfig);
		}
	}

	public abstract void UpdateConfigValue(ConfigEntry configEntry);

	public void UpdateConfigValues(params ConfigEntry[] configEntries)
	{
		if (configEntries != null)
		{
			foreach (ConfigEntry configEntry in configEntries)
			{
				this.UpdateConfigValue(configEntry);
			}
		}
	}

	public void UpdateRegisteredConfigValues()
	{
		foreach (ConfigEntry registeredConfig in this.registeredConfigs)
		{
			this.UpdateConfigValue(registeredConfig);
		}
	}
}

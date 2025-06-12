using System;
using System.Text;
using GameCore;
using NorthwoodLib.Pools;
using Respawning.Waves;

namespace Respawning.Config;

public class StandardWaveConfig<T> : IWaveConfig where T : SpawnableWaveBase
{
	private readonly string _configKey;

	public bool IsEnabled { get; set; } = true;

	public StandardWaveConfig()
	{
		this._configKey = typeof(T).Name.ToLower();
		ConfigFile.OnConfigReloaded = (Action)Delegate.Combine(ConfigFile.OnConfigReloaded, new Action(OnConfigRefresh));
		CustomNetworkManager.OnClientReady += OnConfigRefresh;
	}

	public void Dispose()
	{
		ConfigFile.OnConfigReloaded = (Action)Delegate.Remove(ConfigFile.OnConfigReloaded, new Action(OnConfigRefresh));
		CustomNetworkManager.OnClientReady -= OnConfigRefresh;
	}

	protected string GetConfigPath(string key)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		stringBuilder.Append(this._configKey);
		stringBuilder.Append("_");
		stringBuilder.Append(key);
		return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	protected virtual void OnConfigRefresh()
	{
		this.IsEnabled = ConfigFile.ServerConfig.GetBool(this.GetConfigPath("enabled"), def: true);
	}
}

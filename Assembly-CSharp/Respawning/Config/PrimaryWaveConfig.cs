using GameCore;
using Respawning.Waves;

namespace Respawning.Config;

public class PrimaryWaveConfig<T> : StandardWaveConfig<T> where T : SpawnableWaveBase
{
	private const float SizePercentageDefault = 0.75f;

	public float SizePercentage { get; set; } = 0.75f;

	protected override void OnConfigRefresh()
	{
		base.OnConfigRefresh();
		SizePercentage = ConfigFile.ServerConfig.GetFloat(GetConfigPath("size_percentage"), 0.75f);
	}
}

using NorthwoodLib.Pools;
using Respawning;
using Respawning.Waves;

namespace RemoteAdmin.Communication;

public class RaTeamStatus : RaClientDataRequest
{
	public override int DataId => 8;

	protected override void GatherData()
	{
		StringBuilderPool.Shared.Rent();
		foreach (SpawnableWaveBase wave in WaveManager.Waves)
		{
			AppendData(wave.CreateDebugString().Replace(',', ';'));
		}
	}
}

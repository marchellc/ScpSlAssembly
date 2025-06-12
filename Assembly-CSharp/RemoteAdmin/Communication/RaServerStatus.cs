using CustomPlayerEffects;
using GameCore;

namespace RemoteAdmin.Communication;

public class RaServerStatus : RaClientDataRequest
{
	public override int DataId => 7;

	protected override void GatherData()
	{
		base.AppendData(base.CastBool(RoundSummary.RoundLock));
		base.AppendData(base.CastBool(RoundStart.LobbyLock));
		base.AppendData(base.CastBool(AlphaWarheadController.Singleton != null && AlphaWarheadController.Singleton.IsLocked));
		base.AppendData(base.CastBool(ServerConsole.FriendlyFire));
		base.AppendData(base.CastBool(SpawnProtected.IsProtectionEnabled));
	}
}

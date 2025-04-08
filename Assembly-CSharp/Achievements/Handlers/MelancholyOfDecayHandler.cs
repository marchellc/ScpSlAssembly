using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.PlayableScps.Scp106;

namespace Achievements.Handlers
{
	public class MelancholyOfDecayHandler : AchievementHandlerBase
	{
		internal override void OnInitialize()
		{
			Scp106SinkholeController.OnSubmergeStateChange += this.OnSubmergeStateChange;
			Scp106Attack.OnPlayerTeleported += this.OnPlayerTeleported;
		}

		internal override void OnRoundStarted()
		{
			MelancholyOfDecayHandler.TimeframesPerNetId.Clear();
		}

		private void OnPlayerTeleported(ReferenceHub scp106, ReferenceHub hub)
		{
			double num;
			if (!MelancholyOfDecayHandler.TimeframesPerNetId.TryGetValue(scp106.netId, out num))
			{
				return;
			}
			if (NetworkTime.time > num)
			{
				return;
			}
			AchievementHandlerBase.ServerAchieve(scp106.connectionToClient, AchievementName.MelancholyOfDecay);
		}

		private void OnSubmergeStateChange(Scp106Role scp106, bool newTargetSubmerged)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (newTargetSubmerged || !scp106.TryGetOwner(out referenceHub))
			{
				return;
			}
			MelancholyOfDecayHandler.TimeframesPerNetId[referenceHub.netId] = NetworkTime.time + (double)scp106.Sinkhole.TargetTransitionDuration + 5.0;
		}

		private const int TimeToCapture = 5;

		private static readonly Dictionary<uint, double> TimeframesPerNetId = new Dictionary<uint, double>();
	}
}

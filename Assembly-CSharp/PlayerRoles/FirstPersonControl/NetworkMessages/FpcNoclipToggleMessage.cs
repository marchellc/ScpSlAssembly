using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerStatsSystem;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public struct FpcNoclipToggleMessage : NetworkMessage
	{
		public void ProcessMessage(NetworkConnection sender)
		{
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(sender.identity.netId, out referenceHub))
			{
				return;
			}
			bool flag = FpcNoclip.IsPermitted(referenceHub);
			AdminFlagsStat module = referenceHub.playerStats.GetModule<AdminFlagsStat>();
			PlayerTogglingNoclipEventArgs playerTogglingNoclipEventArgs = new PlayerTogglingNoclipEventArgs(referenceHub, !module.HasFlag(AdminFlags.Noclip));
			playerTogglingNoclipEventArgs.IsAllowed = flag;
			PlayerEvents.OnTogglingNoclip(playerTogglingNoclipEventArgs);
			if (!playerTogglingNoclipEventArgs.IsAllowed)
			{
				return;
			}
			if (referenceHub.roleManager.CurrentRole is IFpcRole)
			{
				module.InvertFlag(AdminFlags.Noclip);
			}
			else
			{
				referenceHub.gameConsoleTransmission.SendToClient("Noclip is not supported for this class.", "yellow");
			}
			PlayerEvents.OnToggledNoclip(new PlayerToggledNoclipEventArgs(referenceHub, module.HasFlag(AdminFlags.Noclip)));
		}
	}
}

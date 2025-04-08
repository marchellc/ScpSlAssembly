using System;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using MEC;

namespace CommandSystem.Commands.RemoteAdmin.Doors
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class LockdownCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "lockdown";

		public string[] Aliases { get; } = new string[] { "ld" };

		public string Description { get; } = "Locks all the doors in the facility.";

		public string[] Usage { get; } = new string[] { "ZoneID Filter", "Duration (Optional)" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.FacilityManagement, out response))
			{
				return false;
			}
			FacilityZone facilityZone = FacilityZone.None;
			float num = 0f;
			if (arguments.Count != 0 && !Enum.TryParse<FacilityZone>(arguments.At(0), true, out facilityZone))
			{
				response = "Specified zone is invalid.";
				return false;
			}
			if (arguments.Count > 1 && !float.TryParse(arguments.At(1), out num))
			{
				response = "Specified duration is invalid.";
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} has locked down the facility (Filter: {1}){2}", sender.LogName, facilityZone, (num == 0f) ? string.Empty : string.Format("for {0} seconds.", num)), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			this.Lockdown(facilityZone, num, num != -1f, out response);
			return true;
		}

		private void Lockdown(FacilityZone zoneToAffect, float duration, bool doLock, out string commandResponse)
		{
			if (RoundSummary.SummaryActive)
			{
				commandResponse = string.Empty;
				return;
			}
			bool flag = zoneToAffect > FacilityZone.None;
			foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
			{
				if (!flag || doorVariant.IsInZone(zoneToAffect))
				{
					doorVariant.ServerChangeLock(DoorLockReason.AdminCommand, doLock);
				}
			}
			Timing.KillCoroutines(new CoroutineHandle[] { this._lockdownHandle });
			if (duration != 0f)
			{
				this._lockdownHandle = Timing.CallDelayed(duration, delegate
				{
					string text;
					this.Lockdown(zoneToAffect, 0f, false, out text);
				});
			}
			commandResponse = ((zoneToAffect == FacilityZone.None) ? ("Locked down the facility" + ((duration == 0f) ? "." : string.Format(" for {0} seconds.", duration))) : string.Format("Locked down {0}{1}", zoneToAffect, (duration == 0f) ? "." : string.Format(" for {0} seconds.", duration)));
		}

		private CoroutineHandle _lockdownHandle;
	}
}

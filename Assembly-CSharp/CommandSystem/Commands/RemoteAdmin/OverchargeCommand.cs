using System;
using MapGeneration;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class OverchargeCommand : ICommand, IUsageProvider
	{
		public string Command { get; } = "overcharge";

		public string[] Aliases { get; } = new string[] { "ocharge", "flicker", "blackout" };

		public string Description { get; } = "Turns lights off in heavy and optionally heavy and light.";

		public string[] Usage { get; } = new string[] { "Zone ID", "Duration" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(PlayerPermissions.RoundEvents, out response))
			{
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To use this, type at least 1 argument(s)! (some parameters are missing)\nUsage: " + arguments.Array[0] + " " + this.DisplayCommandUsage();
				return false;
			}
			bool flag = arguments.Count >= 2;
			FacilityZone facilityZone = FacilityZone.None;
			float num;
			if (!float.TryParse(arguments.At(flag ? 1 : 0), out num))
			{
				response = "Specified duration is invalid.";
				return false;
			}
			if (flag && !Enum.TryParse<FacilityZone>(arguments.At(0), true, out facilityZone))
			{
				response = "Specified zone is invalid.";
				return false;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Administrative, string.Format("{0} has turned off lights in {1} for {2} seconds.", sender.LogName, (facilityZone == FacilityZone.None) ? "the facility" : facilityZone.ToString(), num), ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
			this.Overcharge(facilityZone, num, out response);
			return true;
		}

		private void Overcharge(FacilityZone zoneToAffect, float duration, out string commandResponse)
		{
			bool flag = zoneToAffect > FacilityZone.None;
			foreach (RoomLightController roomLightController in RoomLightController.Instances)
			{
				if (!flag || roomLightController.Room.Zone == zoneToAffect)
				{
					roomLightController.ServerFlickerLights(duration);
				}
			}
			commandResponse = ((zoneToAffect == FacilityZone.None) ? string.Format("Turned off lights in the facility for {0} seconds.", duration) : string.Format("Turned off lights in {0} for {1} seconds.", zoneToAffect, duration));
		}
	}
}

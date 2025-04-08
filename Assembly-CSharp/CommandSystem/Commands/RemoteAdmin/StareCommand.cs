using System;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using PlayerRoles.PlayableScps.Scp096;
using PlayerRoles.PlayableScps.Scp173;
using RemoteAdmin;

namespace CommandSystem.Commands.RemoteAdmin
{
	[CommandHandler(typeof(RemoteAdminCommandHandler))]
	public class StareCommand : ICommand
	{
		public string Command { get; } = "stare";

		public string[] Aliases { get; }

		public string Description { get; } = "Forces yourself to be stared at as SCP-173, look at a fake human as 049-2 or enable rage cycle as 096.";

		public string[] Usage { get; } = new string[] { "Time=Duration" };

		public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
		{
			if (!sender.CheckPermission(new PlayerPermissions[]
			{
				PlayerPermissions.ForceclassSelf,
				PlayerPermissions.ForceclassWithoutRestrictions
			}, out response))
			{
				return false;
			}
			PlayerCommandSender playerCommandSender = sender as PlayerCommandSender;
			if (playerCommandSender == null)
			{
				response = "Only players can run this command.";
				return false;
			}
			if (arguments.Count < 1)
			{
				response = "To execute this command provide at least 1 argument!\nUsage: " + arguments.Array[0] + " " + this.Usage[0];
				return false;
			}
			float num;
			if (!float.TryParse(arguments.Array[1], out num))
			{
				response = string.Format("To execute this command provide the duration!\nUsage: {0} {1}", arguments.Array[0], this.Usage);
				return false;
			}
			PlayerRoleBase currentRole = playerCommandSender.ReferenceHub.roleManager.CurrentRole;
			Scp173Role scp173Role = currentRole as Scp173Role;
			if (scp173Role != null)
			{
				return this.PeanutStare(scp173Role, num, out response);
			}
			ZombieRole zombieRole = currentRole as ZombieRole;
			if (zombieRole != null)
			{
				return this.ZombieStare(zombieRole, num, out response);
			}
			Scp096Role scp096Role = currentRole as Scp096Role;
			if (scp096Role == null)
			{
				ServerLogs.AddLog(ServerLogs.Modules.Administrative, sender.LogName + "'s " + response, ServerLogs.ServerLogType.RemoteAdminActivity_GameChanging, false);
				return true;
			}
			return this.ShyStare(scp096Role, num, out response);
		}

		private bool PeanutStare(Scp173Role scp173, float duration, out string response)
		{
			Scp173ObserversTracker scp173ObserversTracker;
			if (!scp173.SubroutineModule.TryGetSubroutine<Scp173ObserversTracker>(out scp173ObserversTracker))
			{
				response = "SCP-173's observers tracker not found!";
				return false;
			}
			scp173ObserversTracker.SimulatedStare = duration;
			response = "SCP-173 stared at successfully!";
			return true;
		}

		private bool ZombieStare(ZombieRole scp0492, float duration, out string response)
		{
			ZombieBloodlustAbility zombieBloodlustAbility;
			if (!scp0492.SubroutineModule.TryGetSubroutine<ZombieBloodlustAbility>(out zombieBloodlustAbility))
			{
				response = "SCP-049-2's vision tracker not found!";
				return false;
			}
			zombieBloodlustAbility.SimulatedStare = duration;
			response = "SCP-049-2 targeting a fake human successfully!";
			return true;
		}

		private bool ShyStare(Scp096Role scp096, float duration, out string response)
		{
			Scp096RageCycleAbility scp096RageCycleAbility;
			if (!scp096.SubroutineModule.TryGetSubroutine<Scp096RageCycleAbility>(out scp096RageCycleAbility))
			{
				response = "SCP-096's rage cycle ability not found!";
				return false;
			}
			scp096RageCycleAbility.ServerTryEnableInput(duration);
			response = "SCP-096's rage cycle has begun, you can now ENRAGE!";
			return true;
		}
	}
}

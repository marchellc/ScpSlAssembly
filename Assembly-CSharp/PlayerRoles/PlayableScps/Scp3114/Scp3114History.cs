using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Footprinting;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using RoundRestarting;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114History : StandardSubroutine<Scp3114Role>
{
	private record RoundInstance(Footprint OwnerFootprint, List<LoggedIdentity> History)
	{
		public string PrintInstanceHistory(int selfId)
		{
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			stringBuilder.Append("\nIdentity history of ");
			stringBuilder.Append(this.OwnerFootprint.Nickname);
			stringBuilder.Append(", spawned as SCP-3114 [ID: ");
			stringBuilder.Append(selfId.ToString());
			stringBuilder.Append("], ");
			Scp3114History.AppendTime(stringBuilder, this.OwnerFootprint.Stopwatch.Elapsed);
			stringBuilder.Append(" ago:");
			foreach (LoggedIdentity item in this.History)
			{
				item.AppendSelf(stringBuilder);
			}
			return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
		}
	}

	private record LoggedIdentity(string Nickname, RoleTypeId Role, Stopwatch Time)
	{
		public void AppendSelf(StringBuilder sb)
		{
			sb.Append("\n(");
			Scp3114History.AppendTime(sb, this.Time.Elapsed);
			sb.Append(" ago) ");
			if (this.Role == RoleTypeId.None || string.IsNullOrEmpty(this.Nickname))
			{
				sb.Append("<color=red>No disguise</color>");
				return;
			}
			sb.Append(this.Nickname);
			sb.Append(" (");
			if (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this.Role, out var result))
			{
				sb.Append(result.GetColoredName());
			}
			else
			{
				sb.Append("<color=red>Unknown role</color>");
			}
			sb.Append(')');
		}
	}

	private static readonly List<RoundInstance> RoundOverallHistory = new List<RoundInstance>();

	private static int _prevRoundId = -1;

	private int _historyIndex;

	private List<LoggedIdentity> History => Scp3114History.RoundOverallHistory[this._historyIndex].History;

	private void OnStatusChanged()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		switch (base.CastRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
		{
			BasicRagdoll ragdoll = base.CastRole.CurIdentity.Ragdoll;
			if (!(ragdoll == null) && PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(ragdoll.Info.RoleType, out var result))
			{
				this.History.Add(new LoggedIdentity(ragdoll.Info.Nickname, result.RoleTypeId, Stopwatch.StartNew()));
				this.ServerLogIdentity("is now impersonating " + ragdoll.Info.Nickname + ", playing as " + result.RoleName + ".");
			}
			break;
		}
		case Scp3114Identity.DisguiseStatus.None:
			this.History.Add(new LoggedIdentity(null, RoleTypeId.None, Stopwatch.StartNew()));
			this.ServerLogIdentity("is no longer disguised.");
			break;
		}
	}

	private void ServerLogIdentity(string msg)
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		stringBuilder.Append(base.Owner.LoggedNameFromRefHub());
		stringBuilder.Append(", playing as ");
		stringBuilder.Append(base.CastRole.RoleName);
		stringBuilder.Append(", ");
		stringBuilder.Append(msg);
		stringBuilder.Append(' ');
		ServerLogs.AddLog(ServerLogs.Modules.ClassChange, msg, ServerLogs.ServerLogType.GameEvent);
	}

	private static void AppendTime(StringBuilder sb, TimeSpan elapsed)
	{
		sb.Append((int)elapsed.TotalMinutes);
		sb.Append("m ");
		sb.Append(elapsed.Seconds);
		sb.Append("s");
	}

	private static string PrintAllInstances()
	{
		StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
		stringBuilder.Append("Multiple instances of SCP-3114 have been recorded during this round, please run this command again with a specified ID argument.");
		stringBuilder.AppendLine();
		stringBuilder.Append("List of instances, indexed by their ID:");
		for (int i = 0; i < Scp3114History.RoundOverallHistory.Count; i++)
		{
			Footprint ownerFootprint = Scp3114History.RoundOverallHistory[i].OwnerFootprint;
			stringBuilder.AppendLine();
			stringBuilder.Append('[');
			stringBuilder.Append(i.ToString());
			stringBuilder.Append("] -> ");
			stringBuilder.Append(ownerFootprint.Nickname);
			stringBuilder.Append(", spawned ");
			Scp3114History.AppendTime(stringBuilder, ownerFootprint.Stopwatch.Elapsed);
			stringBuilder.Append(" ago.");
		}
		return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
	}

	public static string PrintHistory(int? specificInstance)
	{
		if (Scp3114History.RoundOverallHistory.Count == 0)
		{
			return "There are no recorded SCP-3114 instances this round.";
		}
		if (!specificInstance.HasValue)
		{
			if (Scp3114History.RoundOverallHistory.Count != 1)
			{
				return Scp3114History.PrintAllInstances();
			}
			specificInstance = 0;
		}
		if (!Scp3114History.RoundOverallHistory.TryGet(specificInstance.Value, out var element))
		{
			return "Argument out of range: Invalid instance ID.";
		}
		return element.PrintInstanceHistory(specificInstance.Value);
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		if (Scp3114History._prevRoundId != RoundRestart.UptimeRounds)
		{
			Scp3114History.RoundOverallHistory.Clear();
			Scp3114History._prevRoundId = RoundRestart.UptimeRounds;
		}
		this._historyIndex = Scp3114History.RoundOverallHistory.Count;
		Scp3114History.RoundOverallHistory.Add(new RoundInstance(new Footprint(base.Owner), new List<LoggedIdentity>
		{
			new LoggedIdentity(null, RoleTypeId.None, Stopwatch.StartNew())
		}));
		base.CastRole.CurIdentity.OnStatusChanged += OnStatusChanged;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.History.Clear();
		base.CastRole.CurIdentity.OnStatusChanged -= OnStatusChanged;
	}
}

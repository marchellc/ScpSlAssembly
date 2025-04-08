using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Footprinting;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.Ragdolls;
using PlayerRoles.Subroutines;
using RoundRestarting;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114History : StandardSubroutine<Scp3114Role>
	{
		private List<Scp3114History.LoggedIdentity> History
		{
			get
			{
				return Scp3114History.RoundOverallHistory[this._historyIndex].History;
			}
		}

		private void OnStatusChanged()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			Scp3114Identity.DisguiseStatus status = base.CastRole.CurIdentity.Status;
			if (status != Scp3114Identity.DisguiseStatus.None)
			{
				if (status == Scp3114Identity.DisguiseStatus.Active)
				{
					BasicRagdoll ragdoll = base.CastRole.CurIdentity.Ragdoll;
					PlayerRoleBase playerRoleBase;
					if (!(ragdoll == null) && PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(ragdoll.Info.RoleType, out playerRoleBase))
					{
						this.History.Add(new Scp3114History.LoggedIdentity(ragdoll.Info.Nickname, playerRoleBase.RoleTypeId, Stopwatch.StartNew()));
						this.ServerLogIdentity(string.Concat(new string[]
						{
							"is now impersonating ",
							ragdoll.Info.Nickname,
							", playing as ",
							playerRoleBase.RoleName,
							"."
						}));
						return;
					}
				}
			}
			else
			{
				this.History.Add(new Scp3114History.LoggedIdentity(null, RoleTypeId.None, Stopwatch.StartNew()));
				this.ServerLogIdentity("is no longer disguised.");
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
			ServerLogs.AddLog(ServerLogs.Modules.ClassChange, msg, ServerLogs.ServerLogType.GameEvent, false);
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
			if (specificInstance == null)
			{
				if (Scp3114History.RoundOverallHistory.Count != 1)
				{
					return Scp3114History.PrintAllInstances();
				}
				specificInstance = new int?(0);
			}
			Scp3114History.RoundInstance roundInstance;
			if (!Scp3114History.RoundOverallHistory.TryGet(specificInstance.Value, out roundInstance))
			{
				return "Argument out of range: Invalid instance ID.";
			}
			return roundInstance.PrintInstanceHistory(specificInstance.Value);
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
			Scp3114History.RoundOverallHistory.Add(new Scp3114History.RoundInstance(new Footprint(base.Owner), new List<Scp3114History.LoggedIdentity>
			{
				new Scp3114History.LoggedIdentity(null, RoleTypeId.None, Stopwatch.StartNew())
			}));
			base.CastRole.CurIdentity.OnStatusChanged += this.OnStatusChanged;
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.History.Clear();
			base.CastRole.CurIdentity.OnStatusChanged -= this.OnStatusChanged;
		}

		private static readonly List<Scp3114History.RoundInstance> RoundOverallHistory = new List<Scp3114History.RoundInstance>();

		private static int _prevRoundId = -1;

		private int _historyIndex;

		private class RoundInstance : IEquatable<Scp3114History.RoundInstance>
		{
			public RoundInstance(Footprint OwnerFootprint, List<Scp3114History.LoggedIdentity> History)
			{
				this.OwnerFootprint = OwnerFootprint;
				this.History = History;
				base..ctor();
			}

			[Nullable(1)]
			protected virtual Type EqualityContract
			{
				[NullableContext(1)]
				[CompilerGenerated]
				get
				{
					return typeof(Scp3114History.RoundInstance);
				}
			}

			public Footprint OwnerFootprint { get; set; }

			public List<Scp3114History.LoggedIdentity> History { get; set; }

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
				foreach (Scp3114History.LoggedIdentity loggedIdentity in this.History)
				{
					loggedIdentity.AppendSelf(stringBuilder);
				}
				return StringBuilderPool.Shared.ToStringReturn(stringBuilder);
			}

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("RoundInstance");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}

			[NullableContext(1)]
			protected virtual bool PrintMembers(StringBuilder builder)
			{
				builder.Append("OwnerFootprint");
				builder.Append(" = ");
				builder.Append(this.OwnerFootprint.ToString());
				builder.Append(", ");
				builder.Append("History");
				builder.Append(" = ");
				builder.Append(this.History);
				return true;
			}

			[NullableContext(2)]
			public static bool operator !=(Scp3114History.RoundInstance r1, Scp3114History.RoundInstance r2)
			{
				return !(r1 == r2);
			}

			[NullableContext(2)]
			public static bool operator ==(Scp3114History.RoundInstance r1, Scp3114History.RoundInstance r2)
			{
				return r1 == r2 || (r1 != null && r1.Equals(r2));
			}

			public override int GetHashCode()
			{
				return (EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<Footprint>.Default.GetHashCode(this.<OwnerFootprint>k__BackingField)) * -1521134295 + EqualityComparer<List<Scp3114History.LoggedIdentity>>.Default.GetHashCode(this.<History>k__BackingField);
			}

			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				return this.Equals(obj as Scp3114History.RoundInstance);
			}

			[NullableContext(2)]
			public virtual bool Equals(Scp3114History.RoundInstance other)
			{
				return other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<Footprint>.Default.Equals(this.<OwnerFootprint>k__BackingField, other.<OwnerFootprint>k__BackingField) && EqualityComparer<List<Scp3114History.LoggedIdentity>>.Default.Equals(this.<History>k__BackingField, other.<History>k__BackingField);
			}

			[NullableContext(1)]
			public virtual Scp3114History.RoundInstance <Clone>$()
			{
				return new Scp3114History.RoundInstance(this);
			}

			protected RoundInstance([Nullable(1)] Scp3114History.RoundInstance original)
			{
				this.OwnerFootprint = original.<OwnerFootprint>k__BackingField;
				this.History = original.<History>k__BackingField;
			}

			public void Deconstruct(out Footprint OwnerFootprint, out List<Scp3114History.LoggedIdentity> History)
			{
				OwnerFootprint = this.OwnerFootprint;
				History = this.History;
			}
		}

		private class LoggedIdentity : IEquatable<Scp3114History.LoggedIdentity>
		{
			public LoggedIdentity(string Nickname, RoleTypeId Role, Stopwatch Time)
			{
				this.Nickname = Nickname;
				this.Role = Role;
				this.Time = Time;
				base..ctor();
			}

			[Nullable(1)]
			protected virtual Type EqualityContract
			{
				[NullableContext(1)]
				[CompilerGenerated]
				get
				{
					return typeof(Scp3114History.LoggedIdentity);
				}
			}

			public string Nickname { get; set; }

			public RoleTypeId Role { get; set; }

			public Stopwatch Time { get; set; }

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
				PlayerRoleBase playerRoleBase;
				if (PlayerRoleLoader.TryGetRoleTemplate<PlayerRoleBase>(this.Role, out playerRoleBase))
				{
					sb.Append(playerRoleBase.GetColoredName());
				}
				else
				{
					sb.Append("<color=red>Unknown role</color>");
				}
				sb.Append(')');
			}

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("LoggedIdentity");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}

			[NullableContext(1)]
			protected virtual bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Nickname");
				builder.Append(" = ");
				builder.Append(this.Nickname);
				builder.Append(", ");
				builder.Append("Role");
				builder.Append(" = ");
				builder.Append(this.Role.ToString());
				builder.Append(", ");
				builder.Append("Time");
				builder.Append(" = ");
				builder.Append(this.Time);
				return true;
			}

			[NullableContext(2)]
			public static bool operator !=(Scp3114History.LoggedIdentity r1, Scp3114History.LoggedIdentity r2)
			{
				return !(r1 == r2);
			}

			[NullableContext(2)]
			public static bool operator ==(Scp3114History.LoggedIdentity r1, Scp3114History.LoggedIdentity r2)
			{
				return r1 == r2 || (r1 != null && r1.Equals(r2));
			}

			public override int GetHashCode()
			{
				return ((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.<Nickname>k__BackingField)) * -1521134295 + EqualityComparer<RoleTypeId>.Default.GetHashCode(this.<Role>k__BackingField)) * -1521134295 + EqualityComparer<Stopwatch>.Default.GetHashCode(this.<Time>k__BackingField);
			}

			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				return this.Equals(obj as Scp3114History.LoggedIdentity);
			}

			[NullableContext(2)]
			public virtual bool Equals(Scp3114History.LoggedIdentity other)
			{
				return other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<string>.Default.Equals(this.<Nickname>k__BackingField, other.<Nickname>k__BackingField) && EqualityComparer<RoleTypeId>.Default.Equals(this.<Role>k__BackingField, other.<Role>k__BackingField) && EqualityComparer<Stopwatch>.Default.Equals(this.<Time>k__BackingField, other.<Time>k__BackingField);
			}

			[NullableContext(1)]
			public virtual Scp3114History.LoggedIdentity <Clone>$()
			{
				return new Scp3114History.LoggedIdentity(this);
			}

			protected LoggedIdentity([Nullable(1)] Scp3114History.LoggedIdentity original)
			{
				this.Nickname = original.<Nickname>k__BackingField;
				this.Role = original.<Role>k__BackingField;
				this.Time = original.<Time>k__BackingField;
			}

			public void Deconstruct(out string Nickname, out RoleTypeId Role, out Stopwatch Time)
			{
				Nickname = this.Nickname;
				Role = this.Role;
				Time = this.Time;
			}
		}
	}
}

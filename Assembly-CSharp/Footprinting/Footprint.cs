using System;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using Respawning.NamingRules;

namespace Footprinting
{
	public readonly struct Footprint : IEquatable<Footprint>
	{
		public string UnitName
		{
			get
			{
				HumanRole humanRole;
				if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this.Role, out humanRole))
				{
					return string.Empty;
				}
				List<string> list;
				if (!NamingRulesManager.GeneratedNames.TryGetValue(humanRole.Team, out list))
				{
					return string.Empty;
				}
				int count = list.Count;
				if ((int)this.Unit >= count)
				{
					return string.Empty;
				}
				return list[(int)this.Unit];
			}
		}

		public Footprint(ReferenceHub hub)
		{
			this.IsSet = true;
			this.Stopwatch = Stopwatch.StartNew();
			bool flag = hub != null;
			this.Hub = (flag ? hub : null);
			this.PlayerId = (flag ? hub.PlayerId : 0);
			this.NetId = (flag ? hub.networkIdentity.netId : 0U);
			this.Role = (flag ? hub.GetRoleId() : RoleTypeId.None);
			byte b;
			if (flag)
			{
				HumanRole humanRole = hub.roleManager.CurrentRole as HumanRole;
				if (humanRole != null)
				{
					b = humanRole.UnitNameId;
					goto IL_0083;
				}
			}
			b = 0;
			IL_0083:
			this.Unit = b;
			this.LogUserID = (flag ? hub.authManager.UserId : string.Empty);
			this.Nickname = (flag ? hub.nicknameSync.MyNick : "(null)");
			this.BypassStaff = flag && hub.authManager.BypassBansFlagSet;
			this.IpAddress = ((!flag || !NetworkServer.active || hub.connectionToClient == null) ? null : hub.connectionToClient.address);
			this.LifeIdentifier = (flag ? hub.roleManager.CurrentRole.UniqueLifeIdentifier : 0);
			if (!flag)
			{
				this._serial = 0;
				return;
			}
			if (++Footprint._serialClock == 0)
			{
				Footprint._serialClock++;
			}
			this._serial = Footprint._serialClock;
		}

		public bool Equals(Footprint other)
		{
			return this.IsSet == other.IsSet && this._serial == other._serial;
		}

		public override int GetHashCode()
		{
			return this._serial;
		}

		public readonly ReferenceHub Hub;

		public readonly bool IsSet;

		public readonly int PlayerId;

		public readonly uint NetId;

		public readonly int LifeIdentifier;

		public readonly RoleTypeId Role;

		public readonly byte Unit;

		public readonly string Nickname;

		public readonly string LogUserID;

		public readonly Stopwatch Stopwatch;

		public readonly bool BypassStaff;

		public readonly string IpAddress;

		private readonly int _serial;

		private static int _serialClock;
	}
}

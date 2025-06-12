using System;
using System.Diagnostics;
using Mirror;
using PlayerRoles;
using Respawning.NamingRules;

namespace Footprinting;

public readonly struct Footprint : IEquatable<Footprint>
{
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

	public string UnitName
	{
		get
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this.Role, out var result))
			{
				return string.Empty;
			}
			if (!NamingRulesManager.GeneratedNames.TryGetValue(result.Team, out var value))
			{
				return string.Empty;
			}
			int count = value.Count;
			if (this.Unit >= count)
			{
				return string.Empty;
			}
			return value[this.Unit];
		}
	}

	public Footprint(ReferenceHub hub)
	{
		this.IsSet = true;
		this.Stopwatch = Stopwatch.StartNew();
		bool flag = hub != null;
		this.Hub = (flag ? hub : null);
		this.PlayerId = (flag ? hub.PlayerId : 0);
		this.NetId = (flag ? hub.networkIdentity.netId : 0u);
		this.Role = (flag ? hub.GetRoleId() : RoleTypeId.None);
		this.Unit = (byte)((flag && hub.roleManager.CurrentRole is HumanRole humanRole) ? humanRole.UnitNameId : 0);
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
		if (this.IsSet == other.IsSet)
		{
			return this._serial == other._serial;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return this._serial;
	}
}

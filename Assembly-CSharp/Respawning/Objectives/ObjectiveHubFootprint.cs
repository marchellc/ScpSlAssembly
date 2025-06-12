using Footprinting;
using Mirror;
using PlayerRoles;

namespace Respawning.Objectives;

public struct ObjectiveHubFootprint
{
	private const string DefaultNickname = "???";

	private const RoleTypeId DefaultRole = RoleTypeId.None;

	public string Nickname { get; private set; }

	public RoleTypeId RoleType { get; private set; }

	public ObjectiveHubFootprint(ReferenceHub hub, RoleTypeId role = RoleTypeId.None)
	{
		this.Nickname = ((hub != null) ? hub.nicknameSync.DisplayName : "???");
		this.RoleType = ((role == RoleTypeId.None) ? hub.GetRoleId() : role);
	}

	public ObjectiveHubFootprint(Footprint footprint)
	{
		this.Nickname = footprint.Nickname;
		this.RoleType = footprint.Role;
	}

	public ObjectiveHubFootprint(NetworkReader reader)
	{
		this.Nickname = reader.ReadString();
		this.RoleType = reader.ReadRoleType();
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteString(this.Nickname);
		writer.WriteRoleType(this.RoleType);
	}
}

using Mirror;

namespace PlayerRoles;

public struct RoleSyncInfoPack : NetworkMessage
{
	private readonly ReferenceHub _receiverHub;

	public RoleSyncInfoPack(ReferenceHub receiver)
	{
		this._receiverHub = receiver;
	}

	public RoleSyncInfoPack(NetworkReader reader)
	{
		this._receiverHub = null;
		int num = reader.ReadUShort();
		for (int i = 0; i < num; i++)
		{
			reader.ReadRoleSyncInfo();
		}
	}

	public void WritePlayers(NetworkWriter writer)
	{
		writer.WriteUShort((ushort)ReferenceHub.AllHubs.Count);
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			RoleTypeId roleTypeId = ((allHub.roleManager.CurrentRole is IObfuscatedRole obfuscatedRole) ? obfuscatedRole.GetRoleForUser(this._receiverHub) : allHub.roleManager.CurrentRole.RoleTypeId);
			new RoleSyncInfo(allHub, roleTypeId, this._receiverHub).Write(writer);
			allHub.roleManager.PreviouslySentRole[this._receiverHub.netId] = roleTypeId;
		}
	}
}

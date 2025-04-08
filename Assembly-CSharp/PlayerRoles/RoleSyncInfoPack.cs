using System;
using Mirror;

namespace PlayerRoles
{
	public struct RoleSyncInfoPack : NetworkMessage
	{
		public RoleSyncInfoPack(ReferenceHub receiver)
		{
			this._receiverHub = receiver;
		}

		public RoleSyncInfoPack(NetworkReader reader)
		{
			this._receiverHub = null;
			int num = (int)reader.ReadUShort();
			for (int i = 0; i < num; i++)
			{
				reader.ReadRoleSyncInfo();
			}
		}

		public void WritePlayers(NetworkWriter writer)
		{
			writer.WriteUShort((ushort)ReferenceHub.AllHubs.Count);
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				IObfuscatedRole obfuscatedRole = referenceHub.roleManager.CurrentRole as IObfuscatedRole;
				RoleTypeId roleTypeId = ((obfuscatedRole != null) ? obfuscatedRole.GetRoleForUser(this._receiverHub) : referenceHub.roleManager.CurrentRole.RoleTypeId);
				new RoleSyncInfo(referenceHub, roleTypeId, this._receiverHub).Write(writer);
				referenceHub.roleManager.PreviouslySentRole[this._receiverHub.netId] = roleTypeId;
			}
		}

		private readonly ReferenceHub _receiverHub;
	}
}

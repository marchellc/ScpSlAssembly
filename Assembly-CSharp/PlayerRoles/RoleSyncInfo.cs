using System;
using Mirror;
using PlayerRoles.SpawnData;

namespace PlayerRoles
{
	public struct RoleSyncInfo : NetworkMessage
	{
		public RoleSyncInfo(ReferenceHub target, RoleTypeId role, ReferenceHub receiver)
		{
			this._targetNetId = target.netId;
			this._targetRole = role;
			this._receiverNetId = receiver.netId;
			this._role = target.roleManager.CurrentRole;
		}

		public RoleSyncInfo(NetworkReader reader)
		{
			this._receiverNetId = 0U;
			this._targetNetId = reader.ReadUInt();
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(this._targetNetId, out referenceHub))
			{
				PlayerRolesNetUtils.QueuedRoles[this._targetNetId] = reader;
				this._role = null;
				this._targetRole = RoleTypeId.None;
				return;
			}
			this._targetRole = reader.ReadRoleType();
			if (!NetworkServer.active)
			{
				referenceHub.roleManager.InitializeNewRole(this._targetRole, RoleChangeReason.None, RoleSpawnFlags.All, reader);
			}
			this._role = referenceHub.roleManager.CurrentRole;
		}

		public void Write(NetworkWriter writer)
		{
			writer.WriteUInt(this._targetNetId);
			writer.WriteRoleType(this._targetRole);
			IPublicSpawnDataWriter publicSpawnDataWriter = this._role as IPublicSpawnDataWriter;
			if (publicSpawnDataWriter != null)
			{
				publicSpawnDataWriter.WritePublicSpawnData(writer);
			}
			if (this._receiverNetId != this._targetNetId)
			{
				return;
			}
			IPrivateSpawnDataWriter privateSpawnDataWriter = this._role as IPrivateSpawnDataWriter;
			if (privateSpawnDataWriter != null)
			{
				privateSpawnDataWriter.WritePrivateSpawnData(writer);
			}
		}

		public override string ToString()
		{
			return string.Format("{0} (TargetNetId = '{1}' Role = '{2}')", "RoleSyncInfo", this._targetNetId, this._role);
		}

		private readonly uint _targetNetId;

		private readonly uint _receiverNetId;

		private readonly PlayerRoleBase _role;

		private readonly RoleTypeId _targetRole;
	}
}

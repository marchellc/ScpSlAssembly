using System;
using Mirror;

namespace InventorySystem.Disarming
{
	public static class DisarmMessageSerializers
	{
		public static void Serialize(this NetworkWriter writer, DisarmMessage value)
		{
			writer.WriteUInt(value.PlayerIsNull ? 0U : value.PlayerToDisarm.networkIdentity.netId);
			writer.WriteBool(value.Disarm);
		}

		public static DisarmMessage Deserialize(this NetworkReader reader)
		{
			uint num = reader.ReadUInt();
			bool flag = reader.ReadBool();
			ReferenceHub referenceHub;
			bool flag2 = !ReferenceHub.TryGetHubNetID(num, out referenceHub);
			return new DisarmMessage(referenceHub, flag, flag2);
		}
	}
}

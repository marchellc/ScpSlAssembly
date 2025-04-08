using System;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.Ragdolls
{
	public static class RagdollDataReaderWriter
	{
		public static void WriteRagdollData(this NetworkWriter writer, RagdollData info)
		{
			writer.WriteByte((byte)info.RoleType);
			writer.WriteString(info.Nickname);
			writer.WriteDamageHandler(info.Handler);
			writer.WriteVector3(info.StartPosition);
			writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(info.StartRotation));
			writer.WriteVector3(info.Scale);
			writer.WriteDouble(info.CreationTime);
			writer.WriteUShort(info.Serial);
			writer.WriteReferenceHub(info.OwnerHub);
		}

		public static RagdollData ReadRagdollData(this NetworkReader reader)
		{
			RoleTypeId roleTypeId = (RoleTypeId)reader.ReadByte();
			string text = reader.ReadString();
			DamageHandlerBase damageHandlerBase = reader.ReadDamageHandler();
			Vector3 vector = reader.ReadVector3();
			Quaternion value = reader.ReadLowPrecisionQuaternion().Value;
			Vector3 vector2 = reader.ReadVector3();
			double num = reader.ReadDouble();
			ushort num2 = reader.ReadUShort();
			return new RagdollData(reader.ReadReferenceHub(), damageHandlerBase, roleTypeId, vector, value, vector2, text, num, num2);
		}
	}
}

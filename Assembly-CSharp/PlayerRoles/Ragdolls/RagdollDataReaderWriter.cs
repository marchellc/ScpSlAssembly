using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.Ragdolls;

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
		RoleTypeId roleType = (RoleTypeId)reader.ReadByte();
		string nick = reader.ReadString();
		DamageHandlerBase handler = reader.ReadDamageHandler();
		Vector3 position = reader.ReadVector3();
		Quaternion value = reader.ReadLowPrecisionQuaternion().Value;
		Vector3 scale = reader.ReadVector3();
		double creationTime = reader.ReadDouble();
		ushort value2 = reader.ReadUShort();
		return new RagdollData(reader.ReadReferenceHub(), handler, roleType, position, value, scale, nick, creationTime, value2);
	}
}

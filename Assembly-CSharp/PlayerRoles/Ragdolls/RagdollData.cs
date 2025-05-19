using System;
using System.Collections.Generic;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.Ragdolls;

[Serializable]
public readonly struct RagdollData : NetworkMessage, IEquatable<RagdollData>
{
	public readonly RoleTypeId RoleType;

	public readonly string Nickname;

	public readonly DamageHandlerBase Handler;

	public readonly Vector3 StartPosition;

	public readonly Quaternion StartRotation;

	public readonly Vector3 Scale;

	public readonly double CreationTime;

	public readonly ReferenceHub OwnerHub;

	public readonly ushort Serial;

	public float ExistenceTime => (float)(NetworkTime.time - CreationTime);

	public RagdollData(ReferenceHub hub, DamageHandlerBase handler, Vector3 positionOffset, Quaternion rotationOffset, ushort? serial = 0)
	{
		OwnerHub = hub;
		RoleType = hub.GetRoleId();
		Transform transform = hub.transform;
		StartPosition = transform.position + positionOffset;
		StartRotation = transform.rotation * rotationOffset;
		Nickname = hub.nicknameSync.DisplayName;
		Handler = handler;
		CreationTime = NetworkTime.time;
		Scale = RagdollManager.GetDefaultScale(RoleType);
		Serial = serial ?? RagdollSerialGenerator.GenerateNext();
	}

	public RagdollData(ReferenceHub hub, DamageHandlerBase handler, RoleTypeId roleType, Vector3 position, Quaternion rotation, string nick, double creationTime, ushort? serial = 0)
	{
		OwnerHub = hub;
		RoleType = roleType;
		StartPosition = position;
		StartRotation = rotation;
		Nickname = nick;
		Handler = handler;
		CreationTime = creationTime;
		Scale = RagdollManager.GetDefaultScale(roleType);
		Serial = serial ?? RagdollSerialGenerator.GenerateNext();
	}

	public RagdollData(ReferenceHub hub, DamageHandlerBase handler, RoleTypeId roleType, Vector3 position, Quaternion rotation, Vector3 scale, string nick, double creationTime, ushort? serial = 0)
	{
		OwnerHub = hub;
		RoleType = roleType;
		StartPosition = position;
		StartRotation = rotation;
		Scale = scale;
		Nickname = nick;
		Handler = handler;
		CreationTime = creationTime;
		Serial = serial ?? RagdollSerialGenerator.GenerateNext();
	}

	public bool Equals(RagdollData other)
	{
		if (RoleType == other.RoleType && Nickname == other.Nickname && EqualityComparer<DamageHandlerBase>.Default.Equals(Handler, other.Handler) && StartPosition.Equals(other.StartPosition) && StartRotation.Equals(other.StartRotation) && Scale.Equals(other.Scale) && CreationTime == other.CreationTime)
		{
			return Serial == other.Serial;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is RagdollData other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = (((((byte)RoleType * 397) ^ Nickname.GetHashCode()) * 397) ^ Handler.GetHashCode()) * 397;
		double creationTime = CreationTime;
		return num ^ creationTime.GetHashCode();
	}

	public static bool operator ==(RagdollData left, RagdollData right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RagdollData left, RagdollData right)
	{
		return !left.Equals(right);
	}
}

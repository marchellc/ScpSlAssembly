using System;
using System.Collections.Generic;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.Ragdolls
{
	[Serializable]
	public readonly struct RagdollData : NetworkMessage, IEquatable<RagdollData>
	{
		public float ExistenceTime
		{
			get
			{
				return (float)(NetworkTime.time - this.CreationTime);
			}
		}

		public RagdollData(ReferenceHub hub, DamageHandlerBase handler, Vector3 positionOffset, Quaternion rotationOffset, ushort serial = 0)
		{
			this.OwnerHub = hub;
			this.RoleType = hub.GetRoleId();
			Transform transform = hub.transform;
			this.StartPosition = transform.position + positionOffset;
			this.StartRotation = transform.rotation * rotationOffset;
			this.Nickname = hub.nicknameSync.DisplayName;
			this.Handler = handler;
			this.CreationTime = NetworkTime.time;
			this.Scale = RagdollManager.GetDefaultScale(this.RoleType);
			this.Serial = ((serial == 0) ? RagdollSerialGenerator.GenerateNext() : serial);
		}

		public RagdollData(ReferenceHub hub, DamageHandlerBase handler, RoleTypeId roleType, Vector3 position, Quaternion rotation, string nick, double creationTime, ushort serial = 0)
		{
			this.OwnerHub = hub;
			this.RoleType = roleType;
			this.StartPosition = position;
			this.StartRotation = rotation;
			this.Nickname = nick;
			this.Handler = handler;
			this.CreationTime = creationTime;
			this.Scale = RagdollManager.GetDefaultScale(roleType);
			this.Serial = ((serial == 0) ? RagdollSerialGenerator.GenerateNext() : serial);
		}

		public RagdollData(ReferenceHub hub, DamageHandlerBase handler, RoleTypeId roleType, Vector3 position, Quaternion rotation, Vector3 scale, string nick, double creationTime, ushort serial = 0)
		{
			this.OwnerHub = hub;
			this.RoleType = roleType;
			this.StartPosition = position;
			this.StartRotation = rotation;
			this.Scale = scale;
			this.Nickname = nick;
			this.Handler = handler;
			this.CreationTime = creationTime;
			this.Serial = ((serial == 0) ? RagdollSerialGenerator.GenerateNext() : serial);
		}

		public bool Equals(RagdollData other)
		{
			return this.RoleType == other.RoleType && this.Nickname == other.Nickname && EqualityComparer<DamageHandlerBase>.Default.Equals(this.Handler, other.Handler) && this.StartPosition.Equals(other.StartPosition) && this.StartRotation.Equals(other.StartRotation) && this.Scale.Equals(other.Scale) && this.CreationTime == other.CreationTime && this.Serial == other.Serial;
		}

		public override bool Equals(object obj)
		{
			if (obj is RagdollData)
			{
				RagdollData ragdollData = (RagdollData)obj;
				return this.Equals(ragdollData);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return ((((((int)((byte)this.RoleType) * 397) ^ this.Nickname.GetHashCode()) * 397) ^ this.Handler.GetHashCode()) * 397) ^ this.CreationTime.GetHashCode();
		}

		public static bool operator ==(RagdollData left, RagdollData right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(RagdollData left, RagdollData right)
		{
			return !left.Equals(right);
		}

		public readonly RoleTypeId RoleType;

		public readonly string Nickname;

		public readonly DamageHandlerBase Handler;

		public readonly Vector3 StartPosition;

		public readonly Quaternion StartRotation;

		public readonly Vector3 Scale;

		public readonly double CreationTime;

		public readonly ReferenceHub OwnerHub;

		public readonly ushort Serial;
	}
}

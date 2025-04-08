using System;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl
{
	public static class SyncedGravityMessages
	{
		public static void Serialize(this NetworkWriter writer, SyncedGravityMessages.GravityMessage value)
		{
			writer.WriteReferenceHub(value.TargetHub);
			writer.WriteVector3(value.Gravity);
		}

		public static SyncedGravityMessages.GravityMessage Deserialize(this NetworkReader reader)
		{
			ReferenceHub referenceHub = reader.ReadReferenceHub();
			return new SyncedGravityMessages.GravityMessage(reader.ReadVector3(), referenceHub);
		}

		public readonly struct GravityMessage : NetworkMessage
		{
			public GravityMessage(Vector3 gravity, ReferenceHub targetHub)
			{
				this.Gravity = gravity;
				this.TargetHub = targetHub;
			}

			public readonly Vector3 Gravity;

			public readonly ReferenceHub TargetHub;
		}
	}
}

using System;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public struct FpcFallDamageMessage : NetworkMessage
	{
		public FpcFallDamageMessage(ReferenceHub hub, Vector3 prevPos, RoleTypeId role)
		{
			this._hub = hub;
			this._prevPos = prevPos;
			this._role = role;
		}

		public FpcFallDamageMessage(NetworkReader reader)
		{
			int value = reader.ReadRecyclablePlayerId().Value;
			if (value == 0)
			{
				this._hub = null;
				this._prevPos = reader.ReadRelativePosition().Position;
				this._role = reader.ReadRoleType();
				return;
			}
			this._hub = ReferenceHub.GetHub(value);
			this._prevPos = Vector3.zero;
			this._role = ((this._hub != null) ? this._hub.GetRoleId() : RoleTypeId.None);
		}

		public void Write(NetworkWriter writer)
		{
			if (this._hub == null || !this._hub.IsAlive())
			{
				writer.WriteReferenceHub(null);
				writer.WriteRelativePosition(new RelativePosition(this._prevPos));
				writer.WriteRoleType(this._role);
				return;
			}
			writer.WriteReferenceHub(this._hub);
		}

		public void ProcessMessage()
		{
		}

		private const float SoundDistance = 14f;

		private readonly ReferenceHub _hub;

		private readonly Vector3 _prevPos;

		private readonly RoleTypeId _role;
	}
}

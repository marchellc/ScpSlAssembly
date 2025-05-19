using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.FirstPersonControl.NetworkMessages;

public struct FpcFallDamageMessage : NetworkMessage
{
	private const float SoundDistance = 14f;

	private readonly ReferenceHub _hub;

	private readonly Vector3 _prevPos;

	private readonly RoleTypeId _role;

	public FpcFallDamageMessage(ReferenceHub hub, Vector3 prevPos, RoleTypeId role)
	{
		_hub = hub;
		_prevPos = prevPos;
		_role = role;
	}

	public FpcFallDamageMessage(NetworkReader reader)
	{
		int value = reader.ReadRecyclablePlayerId().Value;
		if (value == 0)
		{
			_hub = null;
			_prevPos = reader.ReadRelativePosition().Position;
			_role = reader.ReadRoleType();
		}
		else
		{
			_hub = ReferenceHub.GetHub(value);
			_prevPos = Vector3.zero;
			_role = ((_hub != null) ? _hub.GetRoleId() : RoleTypeId.None);
		}
	}

	public void Write(NetworkWriter writer)
	{
		if (_hub == null || !_hub.IsAlive())
		{
			writer.WriteReferenceHub(null);
			writer.WriteRelativePosition(new RelativePosition(_prevPos));
			writer.WriteRoleType(_role);
		}
		else
		{
			writer.WriteReferenceHub(_hub);
		}
	}

	public void ProcessMessage()
	{
	}
}

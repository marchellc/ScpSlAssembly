using Mirror;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages;

public readonly struct FpcPositionOverrideMessage : NetworkMessage
{
	public readonly Vector3 Position;

	public FpcPositionOverrideMessage(Vector3 pos)
	{
		this.Position = pos;
	}

	public FpcPositionOverrideMessage(NetworkReader reader)
	{
		this.Position = reader.ReadRelativePosition().Position;
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteRelativePosition(new RelativePosition(this.Position));
	}

	public void ProcessMessage()
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule.ModuleReady)
		{
			fpcRole.FpcModule.Position = this.Position;
		}
	}
}

using System;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public readonly struct FpcPositionOverrideMessage : NetworkMessage
	{
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
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return;
			}
			IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null || !fpcRole.FpcModule.ModuleReady)
			{
				return;
			}
			fpcRole.FpcModule.Position = this.Position;
		}

		public readonly Vector3 Position;
	}
}

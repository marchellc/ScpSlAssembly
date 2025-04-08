using System;
using Mirror;
using RelativePositioning;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public readonly struct FpcFromClientMessage : NetworkMessage
	{
		public FpcFromClientMessage(RelativePosition pos, PlayerMovementState state, bool jump, FpcMouseLook mouseLook)
		{
			this._data = new FpcSyncData(default(FpcSyncData), state, jump, pos, mouseLook);
		}

		public FpcFromClientMessage(NetworkReader reader)
		{
			this._data = new FpcSyncData(reader);
		}

		public void Write(NetworkWriter writer)
		{
			this._data.Write(writer);
		}

		public void ProcessMessage(NetworkConnection sender)
		{
			if (sender.identity.isLocalPlayer)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(sender.identity.netId, out referenceHub))
			{
				return;
			}
			FirstPersonMovementModule firstPersonMovementModule;
			bool flag;
			if (!this._data.TryApply(referenceHub, out firstPersonMovementModule, out flag) || !flag)
			{
				return;
			}
			firstPersonMovementModule.Motor.WantsToJump = true;
		}

		private readonly FpcSyncData _data;
	}
}

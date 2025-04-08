using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Mirror;

namespace PlayerRoles.FirstPersonControl.NetworkMessages
{
	public struct FpcPositionMessage : NetworkMessage
	{
		public FpcPositionMessage(ReferenceHub receiver)
		{
			this._receiver = receiver;
		}

		public FpcPositionMessage(NetworkReader reader)
		{
			this._receiver = null;
			ushort num = reader.ReadUShort();
			FpcPositionMessage.AssignedNetIds.Clear();
			for (int i = 0; i < (int)num; i++)
			{
				int value = reader.ReadRecyclablePlayerId().Value;
				FpcSyncData fpcSyncData = new FpcSyncData(reader);
				ReferenceHub referenceHub;
				if (value != 0 && ReferenceHub.TryGetHub(value, out referenceHub))
				{
					FpcPositionMessage.AssignedNetIds.Add(referenceHub.netId);
					FirstPersonMovementModule firstPersonMovementModule;
					bool flag;
					if (fpcSyncData.TryApply(referenceHub, out firstPersonMovementModule, out flag))
					{
						firstPersonMovementModule.IsGrounded = flag;
					}
				}
			}
			ReferenceHub referenceHub2;
			Scp1344 scp;
			bool flag2 = ReferenceHub.TryGetLocalHub(out referenceHub2) && referenceHub2.playerEffectsController.TryGetEffect<Scp1344>(out scp) && scp.IsEnabled;
			foreach (ReferenceHub referenceHub3 in ReferenceHub.AllHubs)
			{
				IFpcRole fpcRole = referenceHub3.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					bool flag3 = !FpcPositionMessage.AssignedNetIds.Contains(referenceHub3.netId);
					fpcRole.FpcModule.Motor.IsInvisible = flag3;
					if (!flag3 && flag2)
					{
						Invisible invisible;
						fpcRole.FpcModule.Motor.IsInvisible = referenceHub3.playerEffectsController.TryGetEffect<Invisible>(out invisible) && invisible.IsEnabled;
					}
				}
			}
		}

		public void Write(NetworkWriter writer)
		{
			FpcServerPositionDistributor.WriteAll(this._receiver, writer);
		}

		private readonly ReferenceHub _receiver;

		private static readonly HashSet<uint> AssignedNetIds = new HashSet<uint>();
	}
}

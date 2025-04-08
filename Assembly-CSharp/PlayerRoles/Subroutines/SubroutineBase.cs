using System;
using Mirror;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.Subroutines
{
	public abstract class SubroutineBase : MonoBehaviour
	{
		public PlayerRoleBase Role { get; private set; }

		public byte SyncIndex
		{
			get
			{
				if (this._syncIndex != 0)
				{
					return this._syncIndex;
				}
				ISubroutinedRole subroutinedRole = this.Role as ISubroutinedRole;
				if (subroutinedRole == null)
				{
					throw new InvalidOperationException("Could not generate a SyncIndex of '" + base.name + "' subroutine. The role does not derive from ISubroutinedRole!");
				}
				SubroutineBase[] allSubroutines = subroutinedRole.SubroutineModule.AllSubroutines;
				for (int i = 0; i < allSubroutines.Length; i++)
				{
					if (!(allSubroutines[i] != this))
					{
						this._syncIndex = (byte)(i + 1);
						return this._syncIndex;
					}
				}
				throw new InvalidOperationException("Could not generate a SyncIndex of '" + base.name + "' subroutine. It's not on the list of registered subroutines!");
			}
		}

		protected virtual void Awake()
		{
			this.Role = base.GetComponentInParent<PlayerRoleBase>();
		}

		protected virtual void OnValidate()
		{
			SubroutineManagerModule componentInParent = base.GetComponentInParent<SubroutineManagerModule>();
			if (componentInParent == null)
			{
				return;
			}
			componentInParent.AllSubroutines = componentInParent.GetComponentsInChildren<SubroutineBase>();
		}

		protected void ClientSendCmd()
		{
			if (this.Role.Pooled)
			{
				return;
			}
			if (!this.Role.IsLocalPlayer)
			{
				throw new InvalidOperationException("ClientSendCmd can only be called on local player!");
			}
			NetworkClient.Send<SubroutineMessage>(new SubroutineMessage(this, false), 0);
		}

		protected void ServerSendRpc(bool toAll)
		{
			if (!NetworkServer.active || this.Role.Pooled)
			{
				return;
			}
			if (toAll)
			{
				NetworkServer.SendToReady<SubroutineMessage>(new SubroutineMessage(this, true), 0);
				return;
			}
			ReferenceHub referenceHub;
			if (!this.Role.TryGetOwner(out referenceHub))
			{
				return;
			}
			this.ServerSendRpc(referenceHub);
		}

		protected void ServerSendRpc(ReferenceHub target)
		{
			if (!NetworkServer.active || this.Role.Pooled)
			{
				return;
			}
			target.connectionToClient.Send<SubroutineMessage>(new SubroutineMessage(this, true), 0);
		}

		protected void ServerSendRpc(Func<ReferenceHub, bool> condition)
		{
			if (!NetworkServer.active || this.Role.Pooled)
			{
				return;
			}
			new SubroutineMessage(this, true).SendToHubsConditionally(condition, 0);
		}

		public virtual void ClientWriteCmd(NetworkWriter writer)
		{
		}

		public virtual void ServerProcessCmd(NetworkReader reader)
		{
		}

		public virtual void ServerWriteRpc(NetworkWriter writer)
		{
		}

		public virtual void ClientProcessRpc(NetworkReader reader)
		{
		}

		private byte _syncIndex;
	}
}

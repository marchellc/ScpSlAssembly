using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using Mirror;

namespace InventorySystem.Items.Autosync
{
	public abstract class AutosyncItem : ItemBase, IAcquisitionConfirmationTrigger, IAutosyncReceiver
	{
		public bool AcquisitionAlreadyReceived { get; set; }

		public virtual void ServerConfirmAcqusition()
		{
		}

		public virtual void ServerProcessCmd(NetworkReader reader)
		{
		}

		public virtual void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
		}

		public virtual void ClientProcessRpcInstance(NetworkReader reader)
		{
		}

		protected void ClientSendCmd(Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncCmd(base.ItemId, out networkWriter))
			{
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		protected void ServerSendPublicRpc(Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, out networkWriter))
			{
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		protected void ServerSendPrivateRpc(Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, base.Owner, out networkWriter))
			{
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		protected void ServerSendTargetRpc(ReferenceHub receiver, Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, receiver, out networkWriter))
			{
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		protected void ServerSendConditionalRpc(Func<ReferenceHub, bool> receiveCondition, Action<NetworkWriter> extraData = null)
		{
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, receiveCondition, out networkWriter))
			{
				if (extraData != null)
				{
					extraData(networkWriter);
				}
			}
		}

		protected virtual void Awake()
		{
			AutosyncItem.Instances.Add(this);
		}

		protected virtual void OnDestroy()
		{
			AutosyncItem.Instances.Remove(this);
		}

		public override void OnRemoved(ItemPickupBase pickup)
		{
			base.OnRemoved(pickup);
			AutosyncItem.Instances.Remove(this);
		}

		public static readonly HashSet<AutosyncItem> Instances = new HashSet<AutosyncItem>();
	}
}

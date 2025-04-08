using System;
using Mirror;
using Utils.Networking;

namespace InventorySystem.Items.Autosync
{
	public class AutosyncRpc : AutosyncWriterBase
	{
		public AutosyncRpc(ItemIdentifier item, out NetworkWriter writer)
			: base(item, out writer)
		{
			this._mode = AutosyncRpc.Mode.AllClients;
		}

		public AutosyncRpc(ItemIdentifier item, Func<ReferenceHub, bool> predicate, out NetworkWriter writer)
			: base(item, out writer)
		{
			this._mode = AutosyncRpc.Mode.Conditional;
			this._predicate = predicate;
		}

		public AutosyncRpc(ItemIdentifier item, ReferenceHub specificHub, out NetworkWriter writer)
			: base(item, out writer)
		{
			this._mode = AutosyncRpc.Mode.SpecificClient;
			this._specificReceiver = specificHub.connectionToClient;
		}

		public AutosyncRpc(ItemIdentifier item)
		{
			NetworkWriter networkWriter;
			this..ctor(item, out networkWriter);
		}

		public AutosyncRpc(ItemIdentifier item, ReferenceHub receiver)
		{
			NetworkWriter networkWriter;
			this..ctor(item, receiver, out networkWriter);
		}

		public AutosyncRpc(ItemIdentifier item, Func<ReferenceHub, bool> predicate)
		{
			NetworkWriter networkWriter;
			this..ctor(item, predicate, out networkWriter);
		}

		protected override void HandleSending(AutosyncMessage msg)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			switch (this._mode)
			{
			case AutosyncRpc.Mode.SpecificClient:
				this._specificReceiver.Send<AutosyncMessage>(msg, 0);
				return;
			case AutosyncRpc.Mode.AllClients:
				NetworkServer.SendToReady<AutosyncMessage>(msg, 0);
				return;
			case AutosyncRpc.Mode.Conditional:
				msg.SendToHubsConditionally(this._predicate, 0);
				return;
			default:
				return;
			}
		}

		private readonly AutosyncRpc.Mode _mode;

		private readonly Func<ReferenceHub, bool> _predicate;

		private readonly NetworkConnection _specificReceiver;

		private enum Mode
		{
			SpecificClient,
			AllClients,
			Conditional
		}
	}
}

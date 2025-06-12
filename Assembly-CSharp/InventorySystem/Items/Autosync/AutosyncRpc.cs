using System;
using Mirror;
using Utils.Networking;

namespace InventorySystem.Items.Autosync;

public class AutosyncRpc : AutosyncWriterBase
{
	private enum Mode
	{
		SpecificClient,
		AllClients,
		Conditional
	}

	private readonly Mode _mode;

	private readonly Func<ReferenceHub, bool> _predicate;

	private readonly NetworkConnection _specificReceiver;

	public AutosyncRpc(ItemIdentifier item, out NetworkWriter writer)
		: base(item, out writer)
	{
		this._mode = Mode.AllClients;
	}

	public AutosyncRpc(ItemIdentifier item, Func<ReferenceHub, bool> predicate, out NetworkWriter writer)
		: base(item, out writer)
	{
		this._mode = Mode.Conditional;
		this._predicate = predicate;
	}

	public AutosyncRpc(ItemIdentifier item, ReferenceHub specificHub, out NetworkWriter writer)
		: base(item, out writer)
	{
		this._mode = Mode.SpecificClient;
		this._specificReceiver = specificHub.connectionToClient;
	}

	public AutosyncRpc(ItemIdentifier item)
		: this(item, out var _)
	{
	}

	public AutosyncRpc(ItemIdentifier item, ReferenceHub receiver)
		: this(item, receiver, out var _)
	{
	}

	public AutosyncRpc(ItemIdentifier item, Func<ReferenceHub, bool> predicate)
		: this(item, predicate, out var _)
	{
	}

	protected override void HandleSending(AutosyncMessage msg)
	{
		if (NetworkServer.active)
		{
			switch (this._mode)
			{
			case Mode.SpecificClient:
				this._specificReceiver.Send(msg);
				break;
			case Mode.AllClients:
				NetworkServer.SendToReady(msg);
				break;
			case Mode.Conditional:
				msg.SendToHubsConditionally(this._predicate);
				break;
			}
		}
	}
}

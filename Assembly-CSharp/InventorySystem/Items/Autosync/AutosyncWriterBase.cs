using System;
using Mirror;

namespace InventorySystem.Items.Autosync;

public abstract class AutosyncWriterBase : IDisposable
{
	private readonly NetworkWriterPooled _writer;

	private readonly ItemIdentifier _targetItem;

	private bool _alreadySent;

	public AutosyncWriterBase(ItemIdentifier item, out NetworkWriter writer)
	{
		this._alreadySent = false;
		this._writer = NetworkWriterPool.Get();
		this._targetItem = item;
		writer = this._writer;
	}

	public void Dispose()
	{
		this.Send();
	}

	public void Send()
	{
		if (!this._alreadySent)
		{
			this._alreadySent = true;
			this.HandleSending(new AutosyncMessage(this._writer, this._targetItem));
			this._writer.Dispose();
		}
	}

	protected abstract void HandleSending(AutosyncMessage msg);

	public static implicit operator NetworkWriter(AutosyncWriterBase mask)
	{
		return mask._writer;
	}
}

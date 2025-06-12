using LiteNetLib.Utils;

namespace LiteNetLib;

public sealed class NetPacketReader : NetDataReader
{
	private NetPacket _packet;

	private readonly NetManager _manager;

	private readonly NetEvent _evt;

	internal NetPacketReader(NetManager manager, NetEvent evt)
	{
		this._manager = manager;
		this._evt = evt;
	}

	internal void SetSource(NetPacket packet, int headerSize)
	{
		if (packet != null)
		{
			this._packet = packet;
			base.SetSource(packet.RawData, headerSize, packet.Size);
		}
	}

	internal void RecycleInternal()
	{
		base.Clear();
		if (this._packet != null)
		{
			this._manager.PoolRecycle(this._packet);
		}
		this._packet = null;
		this._manager.RecycleEvent(this._evt);
	}

	public void Recycle()
	{
		if (!this._manager.AutoRecycle)
		{
			this.RecycleInternal();
		}
	}
}

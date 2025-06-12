using System.Collections.Generic;
using System.Threading;

namespace LiteNetLib;

internal abstract class BaseChannel
{
	protected readonly NetPeer Peer;

	protected readonly Queue<NetPacket> OutgoingQueue = new Queue<NetPacket>(64);

	private int _isAddedToPeerChannelSendQueue;

	public int PacketsInQueue => this.OutgoingQueue.Count;

	protected BaseChannel(NetPeer peer)
	{
		this.Peer = peer;
	}

	public void AddToQueue(NetPacket packet)
	{
		lock (this.OutgoingQueue)
		{
			this.OutgoingQueue.Enqueue(packet);
		}
		this.AddToPeerChannelSendQueue();
	}

	protected void AddToPeerChannelSendQueue()
	{
		if (Interlocked.CompareExchange(ref this._isAddedToPeerChannelSendQueue, 1, 0) == 0)
		{
			this.Peer.AddToReliableChannelSendQueue(this);
		}
	}

	public bool SendAndCheckQueue()
	{
		bool num = this.SendNextPackets();
		if (!num)
		{
			Interlocked.Exchange(ref this._isAddedToPeerChannelSendQueue, 0);
		}
		return num;
	}

	protected abstract bool SendNextPackets();

	public abstract bool ProcessPacket(NetPacket packet);
}

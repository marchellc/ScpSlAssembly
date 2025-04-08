using System;
using System.Collections.Generic;
using System.Threading;

namespace LiteNetLib
{
	internal abstract class BaseChannel
	{
		public int PacketsInQueue
		{
			get
			{
				return this.OutgoingQueue.Count;
			}
		}

		protected BaseChannel(NetPeer peer)
		{
			this.Peer = peer;
		}

		public void AddToQueue(NetPacket packet)
		{
			Queue<NetPacket> outgoingQueue = this.OutgoingQueue;
			lock (outgoingQueue)
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
			bool flag = this.SendNextPackets();
			if (!flag)
			{
				Interlocked.Exchange(ref this._isAddedToPeerChannelSendQueue, 0);
			}
			return flag;
		}

		protected abstract bool SendNextPackets();

		public abstract bool ProcessPacket(NetPacket packet);

		protected readonly NetPeer Peer;

		protected readonly Queue<NetPacket> OutgoingQueue = new Queue<NetPacket>(64);

		private int _isAddedToPeerChannelSendQueue;
	}
}

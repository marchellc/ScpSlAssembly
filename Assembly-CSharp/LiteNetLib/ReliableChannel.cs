using System;

namespace LiteNetLib;

internal sealed class ReliableChannel : BaseChannel
{
	private struct PendingPacket
	{
		private NetPacket _packet;

		private long _timeStamp;

		private bool _isSent;

		public override string ToString()
		{
			if (this._packet != null)
			{
				return this._packet.Sequence.ToString();
			}
			return "Empty";
		}

		public void Init(NetPacket packet)
		{
			this._packet = packet;
			this._isSent = false;
		}

		public bool TrySend(long currentTime, NetPeer peer)
		{
			if (this._packet == null)
			{
				return false;
			}
			if (this._isSent)
			{
				double num = peer.ResendDelay * 10000.0;
				if ((double)(currentTime - this._timeStamp) < num)
				{
					return true;
				}
			}
			this._timeStamp = currentTime;
			this._isSent = true;
			peer.SendUserData(this._packet);
			return true;
		}

		public bool Clear(NetPeer peer)
		{
			if (this._packet != null)
			{
				peer.RecycleAndDeliver(this._packet);
				this._packet = null;
				return true;
			}
			return false;
		}
	}

	private readonly NetPacket _outgoingAcks;

	private readonly PendingPacket[] _pendingPackets;

	private readonly NetPacket[] _receivedPackets;

	private readonly bool[] _earlyReceived;

	private int _localSeqence;

	private int _remoteSequence;

	private int _localWindowStart;

	private int _remoteWindowStart;

	private bool _mustSendAcks;

	private readonly DeliveryMethod _deliveryMethod;

	private readonly bool _ordered;

	private readonly int _windowSize;

	private const int BitsInByte = 8;

	private readonly byte _id;

	public ReliableChannel(NetPeer peer, bool ordered, byte id)
		: base(peer)
	{
		this._id = id;
		this._windowSize = 64;
		this._ordered = ordered;
		this._pendingPackets = new PendingPacket[this._windowSize];
		for (int i = 0; i < this._pendingPackets.Length; i++)
		{
			this._pendingPackets[i] = default(PendingPacket);
		}
		if (this._ordered)
		{
			this._deliveryMethod = DeliveryMethod.ReliableOrdered;
			this._receivedPackets = new NetPacket[this._windowSize];
		}
		else
		{
			this._deliveryMethod = DeliveryMethod.ReliableUnordered;
			this._earlyReceived = new bool[this._windowSize];
		}
		this._localWindowStart = 0;
		this._localSeqence = 0;
		this._remoteSequence = 0;
		this._remoteWindowStart = 0;
		this._outgoingAcks = new NetPacket(PacketProperty.Ack, (this._windowSize - 1) / 8 + 2)
		{
			ChannelId = id
		};
	}

	private void ProcessAck(NetPacket packet)
	{
		if (packet.Size != this._outgoingAcks.Size)
		{
			return;
		}
		ushort sequence = packet.Sequence;
		int num = NetUtils.RelativeSequenceNumber(this._localWindowStart, sequence);
		if (sequence >= 32768 || num < 0 || num >= this._windowSize)
		{
			return;
		}
		byte[] rawData = packet.RawData;
		lock (this._pendingPackets)
		{
			int num2 = this._localWindowStart;
			while (num2 != this._localSeqence && NetUtils.RelativeSequenceNumber(num2, sequence) < this._windowSize)
			{
				int num3 = num2 % this._windowSize;
				int num4 = 4 + num3 / 8;
				int num5 = num3 % 8;
				if ((rawData[num4] & (1 << num5)) == 0)
				{
					if (base.Peer.NetManager.EnableStatistics)
					{
						base.Peer.Statistics.IncrementPacketLoss();
						base.Peer.NetManager.Statistics.IncrementPacketLoss();
					}
				}
				else
				{
					if (num2 == this._localWindowStart)
					{
						this._localWindowStart = (this._localWindowStart + 1) % 32768;
					}
					this._pendingPackets[num3].Clear(base.Peer);
				}
				num2 = (num2 + 1) % 32768;
			}
		}
	}

	protected override bool SendNextPackets()
	{
		if (this._mustSendAcks)
		{
			this._mustSendAcks = false;
			lock (this._outgoingAcks)
			{
				base.Peer.SendUserData(this._outgoingAcks);
			}
		}
		long ticks = DateTime.UtcNow.Ticks;
		bool flag = false;
		lock (this._pendingPackets)
		{
			lock (base.OutgoingQueue)
			{
				while (base.OutgoingQueue.Count > 0 && NetUtils.RelativeSequenceNumber(this._localSeqence, this._localWindowStart) < this._windowSize)
				{
					NetPacket netPacket = base.OutgoingQueue.Dequeue();
					netPacket.Sequence = (ushort)this._localSeqence;
					netPacket.ChannelId = this._id;
					this._pendingPackets[this._localSeqence % this._windowSize].Init(netPacket);
					this._localSeqence = (this._localSeqence + 1) % 32768;
				}
			}
			for (int num = this._localWindowStart; num != this._localSeqence; num = (num + 1) % 32768)
			{
				if (this._pendingPackets[num % this._windowSize].TrySend(ticks, base.Peer))
				{
					flag = true;
				}
			}
		}
		if (!flag && !this._mustSendAcks)
		{
			return base.OutgoingQueue.Count > 0;
		}
		return true;
	}

	public override bool ProcessPacket(NetPacket packet)
	{
		if (packet.Property == PacketProperty.Ack)
		{
			this.ProcessAck(packet);
			return false;
		}
		int sequence = packet.Sequence;
		if (sequence >= 32768)
		{
			return false;
		}
		int num = NetUtils.RelativeSequenceNumber(sequence, this._remoteWindowStart);
		if (NetUtils.RelativeSequenceNumber(sequence, this._remoteSequence) > this._windowSize)
		{
			return false;
		}
		if (num < 0)
		{
			return false;
		}
		if (num >= this._windowSize * 2)
		{
			return false;
		}
		int num3;
		lock (this._outgoingAcks)
		{
			int num4;
			int num5;
			if (num >= this._windowSize)
			{
				int num2 = (this._remoteWindowStart + num - this._windowSize + 1) % 32768;
				this._outgoingAcks.Sequence = (ushort)num2;
				while (this._remoteWindowStart != num2)
				{
					num3 = this._remoteWindowStart % this._windowSize;
					num4 = 4 + num3 / 8;
					num5 = num3 % 8;
					this._outgoingAcks.RawData[num4] &= (byte)(~(1 << num5));
					this._remoteWindowStart = (this._remoteWindowStart + 1) % 32768;
				}
			}
			this._mustSendAcks = true;
			num3 = sequence % this._windowSize;
			num4 = 4 + num3 / 8;
			num5 = num3 % 8;
			if ((this._outgoingAcks.RawData[num4] & (1 << num5)) != 0)
			{
				base.AddToPeerChannelSendQueue();
				return false;
			}
			this._outgoingAcks.RawData[num4] |= (byte)(1 << num5);
		}
		base.AddToPeerChannelSendQueue();
		if (sequence == this._remoteSequence)
		{
			base.Peer.AddReliablePacket(this._deliveryMethod, packet);
			this._remoteSequence = (this._remoteSequence + 1) % 32768;
			if (this._ordered)
			{
				NetPacket p;
				while ((p = this._receivedPackets[this._remoteSequence % this._windowSize]) != null)
				{
					this._receivedPackets[this._remoteSequence % this._windowSize] = null;
					base.Peer.AddReliablePacket(this._deliveryMethod, p);
					this._remoteSequence = (this._remoteSequence + 1) % 32768;
				}
			}
			else
			{
				while (this._earlyReceived[this._remoteSequence % this._windowSize])
				{
					this._earlyReceived[this._remoteSequence % this._windowSize] = false;
					this._remoteSequence = (this._remoteSequence + 1) % 32768;
				}
			}
			return true;
		}
		if (this._ordered)
		{
			this._receivedPackets[num3] = packet;
		}
		else
		{
			this._earlyReceived[num3] = true;
			base.Peer.AddReliablePacket(this._deliveryMethod, packet);
		}
		return true;
	}
}

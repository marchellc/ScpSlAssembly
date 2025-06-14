using System;

namespace LiteNetLib;

internal sealed class SequencedChannel : BaseChannel
{
	private int _localSequence;

	private ushort _remoteSequence;

	private readonly bool _reliable;

	private NetPacket _lastPacket;

	private readonly NetPacket _ackPacket;

	private bool _mustSendAck;

	private readonly byte _id;

	private long _lastPacketSendTime;

	public SequencedChannel(NetPeer peer, bool reliable, byte id)
		: base(peer)
	{
		this._id = id;
		this._reliable = reliable;
		if (this._reliable)
		{
			this._ackPacket = new NetPacket(PacketProperty.Ack, 0)
			{
				ChannelId = id
			};
		}
	}

	protected override bool SendNextPackets()
	{
		if (this._reliable && base.OutgoingQueue.Count == 0)
		{
			long ticks = DateTime.UtcNow.Ticks;
			if ((double)(ticks - this._lastPacketSendTime) >= base.Peer.ResendDelay * 10000.0)
			{
				NetPacket lastPacket = this._lastPacket;
				if (lastPacket != null)
				{
					this._lastPacketSendTime = ticks;
					base.Peer.SendUserData(lastPacket);
				}
			}
		}
		else
		{
			lock (base.OutgoingQueue)
			{
				while (base.OutgoingQueue.Count > 0)
				{
					NetPacket netPacket = base.OutgoingQueue.Dequeue();
					this._localSequence = (this._localSequence + 1) % 32768;
					netPacket.Sequence = (ushort)this._localSequence;
					netPacket.ChannelId = this._id;
					base.Peer.SendUserData(netPacket);
					if (this._reliable && base.OutgoingQueue.Count == 0)
					{
						this._lastPacketSendTime = DateTime.UtcNow.Ticks;
						this._lastPacket = netPacket;
					}
					else
					{
						base.Peer.NetManager.PoolRecycle(netPacket);
					}
				}
			}
		}
		if (this._reliable && this._mustSendAck)
		{
			this._mustSendAck = false;
			this._ackPacket.Sequence = this._remoteSequence;
			base.Peer.SendUserData(this._ackPacket);
		}
		return this._lastPacket != null;
	}

	public override bool ProcessPacket(NetPacket packet)
	{
		if (packet.IsFragmented)
		{
			return false;
		}
		if (packet.Property == PacketProperty.Ack)
		{
			if (this._reliable && this._lastPacket != null && packet.Sequence == this._lastPacket.Sequence)
			{
				this._lastPacket = null;
			}
			return false;
		}
		int num = NetUtils.RelativeSequenceNumber(packet.Sequence, this._remoteSequence);
		bool result = false;
		if (packet.Sequence < 32768 && num > 0)
		{
			if (base.Peer.NetManager.EnableStatistics)
			{
				base.Peer.Statistics.AddPacketLoss(num - 1);
				base.Peer.NetManager.Statistics.AddPacketLoss(num - 1);
			}
			this._remoteSequence = packet.Sequence;
			base.Peer.NetManager.CreateReceiveEvent(packet, (!this._reliable) ? DeliveryMethod.Sequenced : DeliveryMethod.ReliableSequenced, (byte)(packet.ChannelId / 4), 4, base.Peer);
			result = true;
		}
		if (this._reliable)
		{
			this._mustSendAck = true;
			base.AddToPeerChannelSendQueue();
		}
		return result;
	}
}

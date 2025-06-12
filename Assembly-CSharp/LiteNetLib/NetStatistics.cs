using System.Threading;

namespace LiteNetLib;

public sealed class NetStatistics
{
	private long _packetsSent;

	private long _packetsReceived;

	private long _bytesSent;

	private long _bytesReceived;

	private long _packetLoss;

	public long PacketsSent => Interlocked.Read(ref this._packetsSent);

	public long PacketsReceived => Interlocked.Read(ref this._packetsReceived);

	public long BytesSent => Interlocked.Read(ref this._bytesSent);

	public long BytesReceived => Interlocked.Read(ref this._bytesReceived);

	public long PacketLoss => Interlocked.Read(ref this._packetLoss);

	public long PacketLossPercent
	{
		get
		{
			long packetsSent = this.PacketsSent;
			long packetLoss = this.PacketLoss;
			if (packetsSent != 0L)
			{
				return packetLoss * 100 / packetsSent;
			}
			return 0L;
		}
	}

	public void Reset()
	{
		Interlocked.Exchange(ref this._packetsSent, 0L);
		Interlocked.Exchange(ref this._packetsReceived, 0L);
		Interlocked.Exchange(ref this._bytesSent, 0L);
		Interlocked.Exchange(ref this._bytesReceived, 0L);
		Interlocked.Exchange(ref this._packetLoss, 0L);
	}

	public void IncrementPacketsSent()
	{
		Interlocked.Increment(ref this._packetsSent);
	}

	public void IncrementPacketsReceived()
	{
		Interlocked.Increment(ref this._packetsReceived);
	}

	public void AddBytesSent(long bytesSent)
	{
		Interlocked.Add(ref this._bytesSent, bytesSent);
	}

	public void AddBytesReceived(long bytesReceived)
	{
		Interlocked.Add(ref this._bytesReceived, bytesReceived);
	}

	public void IncrementPacketLoss()
	{
		Interlocked.Increment(ref this._packetLoss);
	}

	public void AddPacketLoss(long packetLoss)
	{
		Interlocked.Add(ref this._packetLoss, packetLoss);
	}

	public override string ToString()
	{
		return $"BytesReceived: {this.BytesReceived}\nPacketsReceived: {this.PacketsReceived}\nBytesSent: {this.BytesSent}\nPacketsSent: {this.PacketsSent}\nPacketLoss: {this.PacketLoss}\nPacketLossPercent: {this.PacketLossPercent}\n";
	}
}

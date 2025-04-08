using System;
using System.Threading;

namespace LiteNetLib
{
	public sealed class NetStatistics
	{
		public long PacketsSent
		{
			get
			{
				return Interlocked.Read(ref this._packetsSent);
			}
		}

		public long PacketsReceived
		{
			get
			{
				return Interlocked.Read(ref this._packetsReceived);
			}
		}

		public long BytesSent
		{
			get
			{
				return Interlocked.Read(ref this._bytesSent);
			}
		}

		public long BytesReceived
		{
			get
			{
				return Interlocked.Read(ref this._bytesReceived);
			}
		}

		public long PacketLoss
		{
			get
			{
				return Interlocked.Read(ref this._packetLoss);
			}
		}

		public long PacketLossPercent
		{
			get
			{
				long packetsSent = this.PacketsSent;
				long packetLoss = this.PacketLoss;
				if (packetsSent != 0L)
				{
					return packetLoss * 100L / packetsSent;
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
			return string.Format("BytesReceived: {0}\nPacketsReceived: {1}\nBytesSent: {2}\nPacketsSent: {3}\nPacketLoss: {4}\nPacketLossPercent: {5}\n", new object[] { this.BytesReceived, this.PacketsReceived, this.BytesSent, this.PacketsSent, this.PacketLoss, this.PacketLossPercent });
		}

		private long _packetsSent;

		private long _packetsReceived;

		private long _bytesSent;

		private long _bytesReceived;

		private long _packetLoss;
	}
}

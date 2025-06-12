using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib.Utils;

namespace LiteNetLib;

public class NetPeer
{
	private class IncomingFragments
	{
		public NetPacket[] Fragments;

		public int ReceivedCount;

		public int TotalSize;

		public byte ChannelId;
	}

	private int _rtt;

	private int _avgRtt;

	private int _rttCount;

	private double _resendDelay = 27.0;

	private int _pingSendTimer;

	private int _rttResetTimer;

	private readonly Stopwatch _pingTimer = new Stopwatch();

	private int _timeSinceLastPacket;

	private long _remoteDelta;

	private readonly object _shutdownLock = new object();

	internal volatile NetPeer NextPeer;

	internal NetPeer PrevPeer;

	private readonly Queue<NetPacket> _unreliableChannel;

	private readonly ConcurrentQueue<BaseChannel> _channelSendQueue;

	private readonly BaseChannel[] _channels;

	private int _mtu;

	private int _mtuIdx;

	private bool _finishMtu;

	private int _mtuCheckTimer;

	private int _mtuCheckAttempts;

	private const int MtuCheckDelay = 1000;

	private const int MaxMtuCheckAttempts = 4;

	private readonly object _mtuMutex = new object();

	private int _fragmentId;

	private readonly Dictionary<ushort, IncomingFragments> _holdedFragments;

	private readonly Dictionary<ushort, ushort> _deliveredFragments;

	private readonly NetPacket _mergeData;

	private int _mergePos;

	private int _mergeCount;

	private IPEndPoint _remoteEndPoint;

	private int _connectAttempts;

	private int _connectTimer;

	private long _connectTime;

	private byte _connectNum;

	private ConnectionState _connectionState;

	private NetPacket _shutdownPacket;

	private const int ShutdownDelay = 300;

	private int _shutdownTimer;

	private readonly NetPacket _pingPacket;

	private readonly NetPacket _pongPacket;

	private readonly NetPacket _connectRequestPacket;

	private readonly NetPacket _connectAcceptPacket;

	public readonly NetManager NetManager;

	public readonly int Id;

	public object Tag;

	public readonly NetStatistics Statistics;

	internal byte ConnectionNum
	{
		get
		{
			return this._connectNum;
		}
		private set
		{
			this._connectNum = value;
			this._mergeData.ConnectionNumber = value;
			this._pingPacket.ConnectionNumber = value;
			this._pongPacket.ConnectionNumber = value;
		}
	}

	public IPEndPoint EndPoint => this._remoteEndPoint;

	public ConnectionState ConnectionState => this._connectionState;

	internal long ConnectTime => this._connectTime;

	public int RemoteId { get; private set; }

	public int Ping => this._avgRtt / 2;

	public int RoundTripTime => this._avgRtt;

	public int Mtu => this._mtu;

	public long RemoteTimeDelta => this._remoteDelta;

	public DateTime RemoteUtcTime => new DateTime(DateTime.UtcNow.Ticks + this._remoteDelta);

	public int TimeSinceLastPacket => this._timeSinceLastPacket;

	internal double ResendDelay => this._resendDelay;

	internal NetPeer(NetManager netManager, IPEndPoint remoteEndPoint, int id)
	{
		this.Id = id;
		this.Statistics = new NetStatistics();
		this.NetManager = netManager;
		this.ResetMtu();
		this._remoteEndPoint = remoteEndPoint;
		this._connectionState = ConnectionState.Connected;
		this._mergeData = new NetPacket(PacketProperty.Merged, NetConstants.MaxPacketSize);
		this._pongPacket = new NetPacket(PacketProperty.Pong, 0);
		this._pingPacket = new NetPacket(PacketProperty.Ping, 0)
		{
			Sequence = 1
		};
		this._unreliableChannel = new Queue<NetPacket>();
		this._holdedFragments = new Dictionary<ushort, IncomingFragments>();
		this._deliveredFragments = new Dictionary<ushort, ushort>();
		this._channels = new BaseChannel[netManager.ChannelsCount * 4];
		this._channelSendQueue = new ConcurrentQueue<BaseChannel>();
	}

	internal void InitiateEndPointChange()
	{
		this.ResetMtu();
		this._connectionState = ConnectionState.EndPointChange;
	}

	internal void FinishEndPointChange(IPEndPoint newEndPoint)
	{
		if (this._connectionState == ConnectionState.EndPointChange)
		{
			this._connectionState = ConnectionState.Connected;
			this._remoteEndPoint = newEndPoint;
		}
	}

	internal void ResetMtu()
	{
		this._finishMtu = false;
		if (this.NetManager.MtuOverride > 0)
		{
			this.OverrideMtu(this.NetManager.MtuOverride);
		}
		else if (this.NetManager.UseSafeMtu)
		{
			this.SetMtu(0);
		}
		else
		{
			this.SetMtu(1);
		}
	}

	private void SetMtu(int mtuIdx)
	{
		this._mtuIdx = mtuIdx;
		this._mtu = NetConstants.PossibleMtu[mtuIdx] - this.NetManager.ExtraPacketSizeForLayer;
	}

	private void OverrideMtu(int mtuValue)
	{
		this._mtu = mtuValue;
		this._finishMtu = true;
	}

	public int GetPacketsCountInReliableQueue(byte channelNumber, bool ordered)
	{
		int num = channelNumber * 4 + (ordered ? 2 : 0);
		BaseChannel baseChannel = this._channels[num];
		if (baseChannel == null)
		{
			return 0;
		}
		return ((ReliableChannel)baseChannel).PacketsInQueue;
	}

	public PooledPacket CreatePacketFromPool(DeliveryMethod deliveryMethod, byte channelNumber)
	{
		int mtu = this._mtu;
		NetPacket netPacket = this.NetManager.PoolGetPacket(mtu);
		if (deliveryMethod == DeliveryMethod.Unreliable)
		{
			netPacket.Property = PacketProperty.Unreliable;
			return new PooledPacket(netPacket, mtu, 0);
		}
		netPacket.Property = PacketProperty.Channeled;
		return new PooledPacket(netPacket, mtu, (byte)((uint)(channelNumber * 4) + (uint)deliveryMethod));
	}

	public void SendPooledPacket(PooledPacket packet, int userDataSize)
	{
		if (this._connectionState != ConnectionState.Connected)
		{
			return;
		}
		packet._packet.Size = packet.UserDataOffset + userDataSize;
		if (packet._packet.Property == PacketProperty.Channeled)
		{
			this.CreateChannel(packet._channelNumber).AddToQueue(packet._packet);
			return;
		}
		lock (this._unreliableChannel)
		{
			this._unreliableChannel.Enqueue(packet._packet);
		}
	}

	private BaseChannel CreateChannel(byte idx)
	{
		BaseChannel baseChannel = this._channels[idx];
		if (baseChannel != null)
		{
			return baseChannel;
		}
		switch ((DeliveryMethod)(byte)(idx % 4))
		{
		case DeliveryMethod.ReliableUnordered:
			baseChannel = new ReliableChannel(this, ordered: false, idx);
			break;
		case DeliveryMethod.Sequenced:
			baseChannel = new SequencedChannel(this, reliable: false, idx);
			break;
		case DeliveryMethod.ReliableOrdered:
			baseChannel = new ReliableChannel(this, ordered: true, idx);
			break;
		case DeliveryMethod.ReliableSequenced:
			baseChannel = new SequencedChannel(this, reliable: true, idx);
			break;
		}
		BaseChannel baseChannel2 = Interlocked.CompareExchange(ref this._channels[idx], baseChannel, null);
		if (baseChannel2 != null)
		{
			return baseChannel2;
		}
		return baseChannel;
	}

	internal NetPeer(NetManager netManager, IPEndPoint remoteEndPoint, int id, byte connectNum, NetDataWriter connectData)
		: this(netManager, remoteEndPoint, id)
	{
		this._connectTime = DateTime.UtcNow.Ticks;
		this._connectionState = ConnectionState.Outgoing;
		this.ConnectionNum = connectNum;
		this._connectRequestPacket = NetConnectRequestPacket.Make(connectData, remoteEndPoint.Serialize(), this._connectTime, id);
		this._connectRequestPacket.ConnectionNumber = connectNum;
		this.NetManager.SendRaw(this._connectRequestPacket, this._remoteEndPoint);
	}

	internal NetPeer(NetManager netManager, ConnectionRequest request, int id)
		: this(netManager, request.RemoteEndPoint, id)
	{
		this._connectTime = request.InternalPacket.ConnectionTime;
		this.ConnectionNum = request.InternalPacket.ConnectionNumber;
		this.RemoteId = request.InternalPacket.PeerId;
		this._connectAcceptPacket = NetConnectAcceptPacket.Make(this._connectTime, this.ConnectionNum, id);
		this._connectionState = ConnectionState.Connected;
		this.NetManager.SendRaw(this._connectAcceptPacket, this._remoteEndPoint);
	}

	internal void Reject(NetConnectRequestPacket requestData, byte[] data, int start, int length)
	{
		this._connectTime = requestData.ConnectionTime;
		this._connectNum = requestData.ConnectionNumber;
		this.Shutdown(data, start, length, force: false);
	}

	internal bool ProcessConnectAccept(NetConnectAcceptPacket packet)
	{
		if (this._connectionState != ConnectionState.Outgoing)
		{
			return false;
		}
		if (packet.ConnectionTime != this._connectTime)
		{
			return false;
		}
		this.ConnectionNum = packet.ConnectionNumber;
		this.RemoteId = packet.PeerId;
		Interlocked.Exchange(ref this._timeSinceLastPacket, 0);
		this._connectionState = ConnectionState.Connected;
		return true;
	}

	public int GetMaxSinglePacketSize(DeliveryMethod options)
	{
		return this._mtu - NetPacket.GetHeaderSize((options != DeliveryMethod.Unreliable) ? PacketProperty.Channeled : PacketProperty.Unreliable);
	}

	public void SendWithDeliveryEvent(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != DeliveryMethod.ReliableUnordered)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		this.SendInternal(data, 0, data.Length, channelNumber, deliveryMethod, userData);
	}

	public void SendWithDeliveryEvent(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != DeliveryMethod.ReliableUnordered)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		this.SendInternal(data, start, length, channelNumber, deliveryMethod, userData);
	}

	public void SendWithDeliveryEvent(NetDataWriter dataWriter, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != DeliveryMethod.ReliableUnordered)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		this.SendInternal(dataWriter.Data, 0, dataWriter.Length, channelNumber, deliveryMethod, userData);
	}

	public void Send(byte[] data, DeliveryMethod deliveryMethod)
	{
		this.SendInternal(data, 0, data.Length, 0, deliveryMethod, null);
	}

	public void Send(NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
	{
		this.SendInternal(dataWriter.Data, 0, dataWriter.Length, 0, deliveryMethod, null);
	}

	public void Send(byte[] data, int start, int length, DeliveryMethod options)
	{
		this.SendInternal(data, start, length, 0, options, null);
	}

	public void Send(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		this.SendInternal(data, 0, data.Length, channelNumber, deliveryMethod, null);
	}

	public void Send(NetDataWriter dataWriter, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		this.SendInternal(dataWriter.Data, 0, dataWriter.Length, channelNumber, deliveryMethod, null);
	}

	public void Send(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		this.SendInternal(data, start, length, channelNumber, deliveryMethod, null);
	}

	private void SendInternal(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (this._connectionState != ConnectionState.Connected || channelNumber >= this._channels.Length)
		{
			return;
		}
		BaseChannel baseChannel = null;
		PacketProperty property;
		if (deliveryMethod == DeliveryMethod.Unreliable)
		{
			property = PacketProperty.Unreliable;
		}
		else
		{
			property = PacketProperty.Channeled;
			baseChannel = this.CreateChannel((byte)((uint)(channelNumber * 4) + (uint)deliveryMethod));
		}
		int headerSize = NetPacket.GetHeaderSize(property);
		int mtu = this._mtu;
		if (length + headerSize > mtu)
		{
			if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != DeliveryMethod.ReliableUnordered)
			{
				throw new TooBigPacketException("Unreliable or ReliableSequenced packet size exceeded maximum of " + (mtu - headerSize) + " bytes, Check allowed size by GetMaxSinglePacketSize()");
			}
			int num = mtu - headerSize - 6;
			int num2 = length / num + ((length % num != 0) ? 1 : 0);
			if (num2 > 65535)
			{
				throw new TooBigPacketException("Data was split in " + num2 + " fragments, which exceeds " + ushort.MaxValue);
			}
			ushort fragmentId = (ushort)Interlocked.Increment(ref this._fragmentId);
			for (ushort num3 = 0; num3 < num2; num3++)
			{
				int num4 = ((length > num) ? num : length);
				NetPacket netPacket = this.NetManager.PoolGetPacket(headerSize + num4 + 6);
				netPacket.Property = property;
				netPacket.UserData = userData;
				netPacket.FragmentId = fragmentId;
				netPacket.FragmentPart = num3;
				netPacket.FragmentsTotal = (ushort)num2;
				netPacket.MarkFragmented();
				Buffer.BlockCopy(data, start + num3 * num, netPacket.RawData, 10, num4);
				baseChannel.AddToQueue(netPacket);
				length -= num4;
			}
			return;
		}
		NetPacket netPacket2 = this.NetManager.PoolGetPacket(headerSize + length);
		netPacket2.Property = property;
		Buffer.BlockCopy(data, start, netPacket2.RawData, headerSize, length);
		netPacket2.UserData = userData;
		if (baseChannel == null)
		{
			lock (this._unreliableChannel)
			{
				this._unreliableChannel.Enqueue(netPacket2);
				return;
			}
		}
		baseChannel.AddToQueue(netPacket2);
	}

	public void Disconnect(byte[] data)
	{
		this.NetManager.DisconnectPeer(this, data);
	}

	public void Disconnect(NetDataWriter writer)
	{
		this.NetManager.DisconnectPeer(this, writer);
	}

	public void Disconnect(byte[] data, int start, int count)
	{
		this.NetManager.DisconnectPeer(this, data, start, count);
	}

	public void Disconnect()
	{
		this.NetManager.DisconnectPeer(this);
	}

	internal DisconnectResult ProcessDisconnect(NetPacket packet)
	{
		if ((this._connectionState == ConnectionState.Connected || this._connectionState == ConnectionState.Outgoing) && packet.Size >= 9 && BitConverter.ToInt64(packet.RawData, 1) == this._connectTime && packet.ConnectionNumber == this._connectNum)
		{
			if (this._connectionState != ConnectionState.Connected)
			{
				return DisconnectResult.Reject;
			}
			return DisconnectResult.Disconnect;
		}
		return DisconnectResult.None;
	}

	internal void AddToReliableChannelSendQueue(BaseChannel channel)
	{
		this._channelSendQueue.Enqueue(channel);
	}

	internal ShutdownResult Shutdown(byte[] data, int start, int length, bool force)
	{
		lock (this._shutdownLock)
		{
			if (this._connectionState == ConnectionState.Disconnected || this._connectionState == ConnectionState.ShutdownRequested)
			{
				return ShutdownResult.None;
			}
			ShutdownResult result = ((this._connectionState != ConnectionState.Connected) ? ShutdownResult.Success : ShutdownResult.WasConnected);
			if (force)
			{
				this._connectionState = ConnectionState.Disconnected;
				return result;
			}
			Interlocked.Exchange(ref this._timeSinceLastPacket, 0);
			this._shutdownPacket = new NetPacket(PacketProperty.Disconnect, length)
			{
				ConnectionNumber = this._connectNum
			};
			FastBitConverter.GetBytes(this._shutdownPacket.RawData, 1, this._connectTime);
			if (this._shutdownPacket.Size >= this._mtu)
			{
				NetDebug.WriteError("[Peer] Disconnect additional data size more than MTU - 8!");
			}
			else if (data != null && length > 0)
			{
				Buffer.BlockCopy(data, start, this._shutdownPacket.RawData, 9, length);
			}
			this._connectionState = ConnectionState.ShutdownRequested;
			this.NetManager.SendRaw(this._shutdownPacket, this._remoteEndPoint);
			return result;
		}
	}

	private void UpdateRoundTripTime(int roundTripTime)
	{
		this._rtt += roundTripTime;
		this._rttCount++;
		this._avgRtt = this._rtt / this._rttCount;
		this._resendDelay = 25.0 + (double)this._avgRtt * 2.1;
	}

	internal void AddReliablePacket(DeliveryMethod method, NetPacket p)
	{
		if (p.IsFragmented)
		{
			ushort fragmentId = p.FragmentId;
			byte channelId = p.ChannelId;
			if (!this._holdedFragments.TryGetValue(fragmentId, out var value))
			{
				value = new IncomingFragments
				{
					Fragments = new NetPacket[p.FragmentsTotal],
					ChannelId = p.ChannelId
				};
				this._holdedFragments.Add(fragmentId, value);
			}
			NetPacket[] fragments = value.Fragments;
			if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null || p.ChannelId != value.ChannelId)
			{
				this.NetManager.PoolRecycle(p);
				NetDebug.WriteError("Invalid fragment packet");
				return;
			}
			fragments[p.FragmentPart] = p;
			value.ReceivedCount++;
			value.TotalSize += p.Size - 10;
			if (value.ReceivedCount != fragments.Length)
			{
				return;
			}
			NetPacket netPacket = this.NetManager.PoolGetPacket(value.TotalSize);
			int num = 0;
			for (int i = 0; i < value.ReceivedCount; i++)
			{
				NetPacket netPacket2 = fragments[i];
				int num2 = netPacket2.Size - 10;
				if (num + num2 > netPacket.RawData.Length)
				{
					this._holdedFragments.Remove(fragmentId);
					NetDebug.WriteError($"Fragment error pos: {num + num2} >= resultPacketSize: {netPacket.RawData.Length} , totalSize: {value.TotalSize}");
					return;
				}
				if (netPacket2.Size > netPacket2.RawData.Length)
				{
					this._holdedFragments.Remove(fragmentId);
					NetDebug.WriteError($"Fragment error size: {netPacket2.Size} > fragment.RawData.Length: {netPacket2.RawData.Length}");
					return;
				}
				Buffer.BlockCopy(netPacket2.RawData, 10, netPacket.RawData, num, num2);
				num += num2;
				this.NetManager.PoolRecycle(netPacket2);
				fragments[i] = null;
			}
			this._holdedFragments.Remove(fragmentId);
			this.NetManager.CreateReceiveEvent(netPacket, method, (byte)(channelId / 4), 0, this);
		}
		else
		{
			this.NetManager.CreateReceiveEvent(p, method, (byte)(p.ChannelId / 4), 4, this);
		}
	}

	private void ProcessMtuPacket(NetPacket packet)
	{
		if (packet.Size < NetConstants.PossibleMtu[0])
		{
			return;
		}
		int num = BitConverter.ToInt32(packet.RawData, 1);
		int num2 = BitConverter.ToInt32(packet.RawData, packet.Size - 4);
		if (num != packet.Size || num != num2 || num > NetConstants.MaxPacketSize)
		{
			NetDebug.WriteError($"[MTU] Broken packet. RMTU {num}, EMTU {num2}, PSIZE {packet.Size}");
		}
		else if (packet.Property == PacketProperty.MtuCheck)
		{
			this._mtuCheckAttempts = 0;
			packet.Property = PacketProperty.MtuOk;
			this.NetManager.SendRawAndRecycle(packet, this._remoteEndPoint);
		}
		else if (num > this._mtu && !this._finishMtu && num == NetConstants.PossibleMtu[this._mtuIdx + 1] - this.NetManager.ExtraPacketSizeForLayer)
		{
			lock (this._mtuMutex)
			{
				this.SetMtu(this._mtuIdx + 1);
			}
			if (this._mtuIdx == NetConstants.PossibleMtu.Length - 1)
			{
				this._finishMtu = true;
			}
			this.NetManager.PoolRecycle(packet);
		}
	}

	private void UpdateMtuLogic(int deltaTime)
	{
		if (this._finishMtu)
		{
			return;
		}
		this._mtuCheckTimer += deltaTime;
		if (this._mtuCheckTimer < 1000)
		{
			return;
		}
		this._mtuCheckTimer = 0;
		this._mtuCheckAttempts++;
		if (this._mtuCheckAttempts >= 4)
		{
			this._finishMtu = true;
			return;
		}
		lock (this._mtuMutex)
		{
			if (this._mtuIdx < NetConstants.PossibleMtu.Length - 1)
			{
				int num = NetConstants.PossibleMtu[this._mtuIdx + 1] - this.NetManager.ExtraPacketSizeForLayer;
				NetPacket netPacket = this.NetManager.PoolGetPacket(num);
				netPacket.Property = PacketProperty.MtuCheck;
				FastBitConverter.GetBytes(netPacket.RawData, 1, num);
				FastBitConverter.GetBytes(netPacket.RawData, netPacket.Size - 4, num);
				if (this.NetManager.SendRawAndRecycle(netPacket, this._remoteEndPoint) <= 0)
				{
					this._finishMtu = true;
				}
			}
		}
	}

	internal ConnectRequestResult ProcessConnectRequest(NetConnectRequestPacket connRequest)
	{
		switch (this._connectionState)
		{
		case ConnectionState.Outgoing:
		{
			if (connRequest.ConnectionTime < this._connectTime)
			{
				return ConnectRequestResult.P2PLose;
			}
			if (connRequest.ConnectionTime != this._connectTime)
			{
				break;
			}
			SocketAddress socketAddress = this._remoteEndPoint.Serialize();
			byte[] targetAddress = connRequest.TargetAddress;
			for (int num = socketAddress.Size - 1; num >= 0; num--)
			{
				byte b = socketAddress[num];
				if (b != targetAddress[num] && b < targetAddress[num])
				{
					return ConnectRequestResult.P2PLose;
				}
			}
			break;
		}
		case ConnectionState.Connected:
			if (connRequest.ConnectionTime == this._connectTime)
			{
				this.NetManager.SendRaw(this._connectAcceptPacket, this._remoteEndPoint);
			}
			else if (connRequest.ConnectionTime > this._connectTime)
			{
				return ConnectRequestResult.Reconnection;
			}
			break;
		case ConnectionState.ShutdownRequested:
		case ConnectionState.Disconnected:
			if (connRequest.ConnectionTime >= this._connectTime)
			{
				return ConnectRequestResult.NewConnection;
			}
			break;
		}
		return ConnectRequestResult.None;
	}

	internal void ProcessPacket(NetPacket packet)
	{
		if (this._connectionState == ConnectionState.Outgoing || this._connectionState == ConnectionState.Disconnected)
		{
			this.NetManager.PoolRecycle(packet);
			return;
		}
		if (packet.Property == PacketProperty.ShutdownOk)
		{
			if (this._connectionState == ConnectionState.ShutdownRequested)
			{
				this._connectionState = ConnectionState.Disconnected;
			}
			this.NetManager.PoolRecycle(packet);
			return;
		}
		if (packet.ConnectionNumber != this._connectNum)
		{
			this.NetManager.PoolRecycle(packet);
			return;
		}
		Interlocked.Exchange(ref this._timeSinceLastPacket, 0);
		switch (packet.Property)
		{
		case PacketProperty.Merged:
		{
			int num2 = 1;
			while (num2 < packet.Size)
			{
				ushort num3 = BitConverter.ToUInt16(packet.RawData, num2);
				if (num3 == 0)
				{
					break;
				}
				num2 += 2;
				if (packet.RawData.Length - num2 < num3)
				{
					break;
				}
				NetPacket netPacket = this.NetManager.PoolGetPacket(num3);
				Buffer.BlockCopy(packet.RawData, num2, netPacket.RawData, 0, num3);
				netPacket.Size = num3;
				if (!netPacket.Verify())
				{
					break;
				}
				num2 += num3;
				this.ProcessPacket(netPacket);
			}
			this.NetManager.PoolRecycle(packet);
			break;
		}
		case PacketProperty.Ping:
			if (NetUtils.RelativeSequenceNumber(packet.Sequence, this._pongPacket.Sequence) > 0)
			{
				FastBitConverter.GetBytes(this._pongPacket.RawData, 3, DateTime.UtcNow.Ticks);
				this._pongPacket.Sequence = packet.Sequence;
				this.NetManager.SendRaw(this._pongPacket, this._remoteEndPoint);
			}
			this.NetManager.PoolRecycle(packet);
			break;
		case PacketProperty.Pong:
			if (packet.Sequence == this._pingPacket.Sequence)
			{
				this._pingTimer.Stop();
				int num = (int)this._pingTimer.ElapsedMilliseconds;
				this._remoteDelta = BitConverter.ToInt64(packet.RawData, 3) + (long)num * 10000L / 2 - DateTime.UtcNow.Ticks;
				this.UpdateRoundTripTime(num);
				this.NetManager.ConnectionLatencyUpdated(this, num / 2);
			}
			this.NetManager.PoolRecycle(packet);
			break;
		case PacketProperty.Channeled:
		case PacketProperty.Ack:
		{
			if (packet.ChannelId > this._channels.Length)
			{
				this.NetManager.PoolRecycle(packet);
				break;
			}
			BaseChannel baseChannel = this._channels[packet.ChannelId] ?? ((packet.Property == PacketProperty.Ack) ? null : this.CreateChannel(packet.ChannelId));
			if (baseChannel != null && !baseChannel.ProcessPacket(packet))
			{
				this.NetManager.PoolRecycle(packet);
			}
			break;
		}
		case PacketProperty.Unreliable:
			this.NetManager.CreateReceiveEvent(packet, DeliveryMethod.Unreliable, 0, 1, this);
			break;
		case PacketProperty.MtuCheck:
		case PacketProperty.MtuOk:
			this.ProcessMtuPacket(packet);
			break;
		default:
			NetDebug.WriteError("Error! Unexpected packet type: " + packet.Property);
			break;
		}
	}

	private void SendMerged()
	{
		if (this._mergeCount != 0)
		{
			int num = ((this._mergeCount <= 1) ? this.NetManager.SendRaw(this._mergeData.RawData, 3, this._mergePos - 2, this._remoteEndPoint) : this.NetManager.SendRaw(this._mergeData.RawData, 0, 1 + this._mergePos, this._remoteEndPoint));
			if (this.NetManager.EnableStatistics)
			{
				this.Statistics.IncrementPacketsSent();
				this.Statistics.AddBytesSent(num);
			}
			this._mergePos = 0;
			this._mergeCount = 0;
		}
	}

	internal void SendUserData(NetPacket packet)
	{
		packet.ConnectionNumber = this._connectNum;
		int num = 1 + packet.Size + 2;
		if (num + 20 >= this._mtu)
		{
			int num2 = this.NetManager.SendRaw(packet, this._remoteEndPoint);
			if (this.NetManager.EnableStatistics)
			{
				this.Statistics.IncrementPacketsSent();
				this.Statistics.AddBytesSent(num2);
			}
			return;
		}
		if (this._mergePos + num > this._mtu)
		{
			this.SendMerged();
		}
		FastBitConverter.GetBytes(this._mergeData.RawData, this._mergePos + 1, (ushort)packet.Size);
		Buffer.BlockCopy(packet.RawData, 0, this._mergeData.RawData, this._mergePos + 1 + 2, packet.Size);
		this._mergePos += packet.Size + 2;
		this._mergeCount++;
	}

	internal void Update(int deltaTime)
	{
		Interlocked.Add(ref this._timeSinceLastPacket, deltaTime);
		switch (this._connectionState)
		{
		case ConnectionState.Connected:
			if (this._timeSinceLastPacket > this.NetManager.DisconnectTimeout)
			{
				this.NetManager.DisconnectPeerForce(this, DisconnectReason.Timeout, SocketError.Success, null);
				return;
			}
			break;
		case ConnectionState.ShutdownRequested:
			if (this._timeSinceLastPacket > this.NetManager.DisconnectTimeout)
			{
				this._connectionState = ConnectionState.Disconnected;
				return;
			}
			this._shutdownTimer += deltaTime;
			if (this._shutdownTimer >= 300)
			{
				this._shutdownTimer = 0;
				this.NetManager.SendRaw(this._shutdownPacket, this._remoteEndPoint);
			}
			return;
		case ConnectionState.Outgoing:
			this._connectTimer += deltaTime;
			if (this._connectTimer > this.NetManager.ReconnectDelay)
			{
				this._connectTimer = 0;
				this._connectAttempts++;
				if (this._connectAttempts > this.NetManager.MaxConnectAttempts)
				{
					this.NetManager.DisconnectPeerForce(this, DisconnectReason.ConnectionFailed, SocketError.Success, null);
				}
				else
				{
					this.NetManager.SendRaw(this._connectRequestPacket, this._remoteEndPoint);
				}
			}
			return;
		case ConnectionState.Disconnected:
			return;
		}
		this._pingSendTimer += deltaTime;
		if (this._pingSendTimer >= this.NetManager.PingInterval)
		{
			this._pingSendTimer = 0;
			this._pingPacket.Sequence++;
			if (this._pingTimer.IsRunning)
			{
				this.UpdateRoundTripTime((int)this._pingTimer.ElapsedMilliseconds);
			}
			this._pingTimer.Restart();
			this.NetManager.SendRaw(this._pingPacket, this._remoteEndPoint);
		}
		this._rttResetTimer += deltaTime;
		if (this._rttResetTimer >= this.NetManager.PingInterval * 3)
		{
			this._rttResetTimer = 0;
			this._rtt = this._avgRtt;
			this._rttCount = 1;
		}
		this.UpdateMtuLogic(deltaTime);
		int count = this._channelSendQueue.Count;
		BaseChannel result;
		while (count-- > 0 && this._channelSendQueue.TryDequeue(out result))
		{
			if (result.SendAndCheckQueue())
			{
				this._channelSendQueue.Enqueue(result);
			}
		}
		lock (this._unreliableChannel)
		{
			int count2 = this._unreliableChannel.Count;
			for (int i = 0; i < count2; i++)
			{
				NetPacket packet = this._unreliableChannel.Dequeue();
				this.SendUserData(packet);
				this.NetManager.PoolRecycle(packet);
			}
		}
		this.SendMerged();
	}

	internal void RecycleAndDeliver(NetPacket packet)
	{
		if (packet.UserData != null)
		{
			if (packet.IsFragmented)
			{
				this._deliveredFragments.TryGetValue(packet.FragmentId, out var value);
				value++;
				if (value == packet.FragmentsTotal)
				{
					this.NetManager.MessageDelivered(this, packet.UserData);
					this._deliveredFragments.Remove(packet.FragmentId);
				}
				else
				{
					this._deliveredFragments[packet.FragmentId] = value;
				}
			}
			else
			{
				this.NetManager.MessageDelivered(this, packet.UserData);
			}
			packet.UserData = null;
		}
		this.NetManager.PoolRecycle(packet);
	}
}

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
			return _connectNum;
		}
		private set
		{
			_connectNum = value;
			_mergeData.ConnectionNumber = value;
			_pingPacket.ConnectionNumber = value;
			_pongPacket.ConnectionNumber = value;
		}
	}

	public IPEndPoint EndPoint => _remoteEndPoint;

	public ConnectionState ConnectionState => _connectionState;

	internal long ConnectTime => _connectTime;

	public int RemoteId { get; private set; }

	public int Ping => _avgRtt / 2;

	public int RoundTripTime => _avgRtt;

	public int Mtu => _mtu;

	public long RemoteTimeDelta => _remoteDelta;

	public DateTime RemoteUtcTime => new DateTime(DateTime.UtcNow.Ticks + _remoteDelta);

	public int TimeSinceLastPacket => _timeSinceLastPacket;

	internal double ResendDelay => _resendDelay;

	internal NetPeer(NetManager netManager, IPEndPoint remoteEndPoint, int id)
	{
		Id = id;
		Statistics = new NetStatistics();
		NetManager = netManager;
		ResetMtu();
		_remoteEndPoint = remoteEndPoint;
		_connectionState = ConnectionState.Connected;
		_mergeData = new NetPacket(PacketProperty.Merged, NetConstants.MaxPacketSize);
		_pongPacket = new NetPacket(PacketProperty.Pong, 0);
		_pingPacket = new NetPacket(PacketProperty.Ping, 0)
		{
			Sequence = 1
		};
		_unreliableChannel = new Queue<NetPacket>();
		_holdedFragments = new Dictionary<ushort, IncomingFragments>();
		_deliveredFragments = new Dictionary<ushort, ushort>();
		_channels = new BaseChannel[netManager.ChannelsCount * 4];
		_channelSendQueue = new ConcurrentQueue<BaseChannel>();
	}

	internal void InitiateEndPointChange()
	{
		ResetMtu();
		_connectionState = ConnectionState.EndPointChange;
	}

	internal void FinishEndPointChange(IPEndPoint newEndPoint)
	{
		if (_connectionState == ConnectionState.EndPointChange)
		{
			_connectionState = ConnectionState.Connected;
			_remoteEndPoint = newEndPoint;
		}
	}

	internal void ResetMtu()
	{
		_finishMtu = false;
		if (NetManager.MtuOverride > 0)
		{
			OverrideMtu(NetManager.MtuOverride);
		}
		else if (NetManager.UseSafeMtu)
		{
			SetMtu(0);
		}
		else
		{
			SetMtu(1);
		}
	}

	private void SetMtu(int mtuIdx)
	{
		_mtuIdx = mtuIdx;
		_mtu = NetConstants.PossibleMtu[mtuIdx] - NetManager.ExtraPacketSizeForLayer;
	}

	private void OverrideMtu(int mtuValue)
	{
		_mtu = mtuValue;
		_finishMtu = true;
	}

	public int GetPacketsCountInReliableQueue(byte channelNumber, bool ordered)
	{
		int num = channelNumber * 4 + (ordered ? 2 : 0);
		BaseChannel baseChannel = _channels[num];
		if (baseChannel == null)
		{
			return 0;
		}
		return ((ReliableChannel)baseChannel).PacketsInQueue;
	}

	public PooledPacket CreatePacketFromPool(DeliveryMethod deliveryMethod, byte channelNumber)
	{
		int mtu = _mtu;
		NetPacket netPacket = NetManager.PoolGetPacket(mtu);
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
		if (_connectionState != ConnectionState.Connected)
		{
			return;
		}
		packet._packet.Size = packet.UserDataOffset + userDataSize;
		if (packet._packet.Property == PacketProperty.Channeled)
		{
			CreateChannel(packet._channelNumber).AddToQueue(packet._packet);
			return;
		}
		lock (_unreliableChannel)
		{
			_unreliableChannel.Enqueue(packet._packet);
		}
	}

	private BaseChannel CreateChannel(byte idx)
	{
		BaseChannel baseChannel = _channels[idx];
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
		BaseChannel baseChannel2 = Interlocked.CompareExchange(ref _channels[idx], baseChannel, null);
		if (baseChannel2 != null)
		{
			return baseChannel2;
		}
		return baseChannel;
	}

	internal NetPeer(NetManager netManager, IPEndPoint remoteEndPoint, int id, byte connectNum, NetDataWriter connectData)
		: this(netManager, remoteEndPoint, id)
	{
		_connectTime = DateTime.UtcNow.Ticks;
		_connectionState = ConnectionState.Outgoing;
		ConnectionNum = connectNum;
		_connectRequestPacket = NetConnectRequestPacket.Make(connectData, remoteEndPoint.Serialize(), _connectTime, id);
		_connectRequestPacket.ConnectionNumber = connectNum;
		NetManager.SendRaw(_connectRequestPacket, _remoteEndPoint);
	}

	internal NetPeer(NetManager netManager, ConnectionRequest request, int id)
		: this(netManager, request.RemoteEndPoint, id)
	{
		_connectTime = request.InternalPacket.ConnectionTime;
		ConnectionNum = request.InternalPacket.ConnectionNumber;
		RemoteId = request.InternalPacket.PeerId;
		_connectAcceptPacket = NetConnectAcceptPacket.Make(_connectTime, ConnectionNum, id);
		_connectionState = ConnectionState.Connected;
		NetManager.SendRaw(_connectAcceptPacket, _remoteEndPoint);
	}

	internal void Reject(NetConnectRequestPacket requestData, byte[] data, int start, int length)
	{
		_connectTime = requestData.ConnectionTime;
		_connectNum = requestData.ConnectionNumber;
		Shutdown(data, start, length, force: false);
	}

	internal bool ProcessConnectAccept(NetConnectAcceptPacket packet)
	{
		if (_connectionState != ConnectionState.Outgoing)
		{
			return false;
		}
		if (packet.ConnectionTime != _connectTime)
		{
			return false;
		}
		ConnectionNum = packet.ConnectionNumber;
		RemoteId = packet.PeerId;
		Interlocked.Exchange(ref _timeSinceLastPacket, 0);
		_connectionState = ConnectionState.Connected;
		return true;
	}

	public int GetMaxSinglePacketSize(DeliveryMethod options)
	{
		return _mtu - NetPacket.GetHeaderSize((options != DeliveryMethod.Unreliable) ? PacketProperty.Channeled : PacketProperty.Unreliable);
	}

	public void SendWithDeliveryEvent(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != 0)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		SendInternal(data, 0, data.Length, channelNumber, deliveryMethod, userData);
	}

	public void SendWithDeliveryEvent(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != 0)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		SendInternal(data, start, length, channelNumber, deliveryMethod, userData);
	}

	public void SendWithDeliveryEvent(NetDataWriter dataWriter, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != 0)
		{
			throw new ArgumentException("Delivery event will work only for ReliableOrdered/Unordered packets");
		}
		SendInternal(dataWriter.Data, 0, dataWriter.Length, channelNumber, deliveryMethod, userData);
	}

	public void Send(byte[] data, DeliveryMethod deliveryMethod)
	{
		SendInternal(data, 0, data.Length, 0, deliveryMethod, null);
	}

	public void Send(NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
	{
		SendInternal(dataWriter.Data, 0, dataWriter.Length, 0, deliveryMethod, null);
	}

	public void Send(byte[] data, int start, int length, DeliveryMethod options)
	{
		SendInternal(data, start, length, 0, options, null);
	}

	public void Send(byte[] data, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		SendInternal(data, 0, data.Length, channelNumber, deliveryMethod, null);
	}

	public void Send(NetDataWriter dataWriter, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		SendInternal(dataWriter.Data, 0, dataWriter.Length, channelNumber, deliveryMethod, null);
	}

	public void Send(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod)
	{
		SendInternal(data, start, length, channelNumber, deliveryMethod, null);
	}

	private void SendInternal(byte[] data, int start, int length, byte channelNumber, DeliveryMethod deliveryMethod, object userData)
	{
		if (_connectionState != ConnectionState.Connected || channelNumber >= _channels.Length)
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
			baseChannel = CreateChannel((byte)((uint)(channelNumber * 4) + (uint)deliveryMethod));
		}
		int headerSize = NetPacket.GetHeaderSize(property);
		int mtu = _mtu;
		if (length + headerSize > mtu)
		{
			if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != 0)
			{
				throw new TooBigPacketException("Unreliable or ReliableSequenced packet size exceeded maximum of " + (mtu - headerSize) + " bytes, Check allowed size by GetMaxSinglePacketSize()");
			}
			int num = mtu - headerSize - 6;
			int num2 = length / num + ((length % num != 0) ? 1 : 0);
			if (num2 > 65535)
			{
				throw new TooBigPacketException("Data was split in " + num2 + " fragments, which exceeds " + ushort.MaxValue);
			}
			ushort fragmentId = (ushort)Interlocked.Increment(ref _fragmentId);
			for (ushort num3 = 0; num3 < num2; num3++)
			{
				int num4 = ((length > num) ? num : length);
				NetPacket netPacket = NetManager.PoolGetPacket(headerSize + num4 + 6);
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
		NetPacket netPacket2 = NetManager.PoolGetPacket(headerSize + length);
		netPacket2.Property = property;
		Buffer.BlockCopy(data, start, netPacket2.RawData, headerSize, length);
		netPacket2.UserData = userData;
		if (baseChannel == null)
		{
			lock (_unreliableChannel)
			{
				_unreliableChannel.Enqueue(netPacket2);
				return;
			}
		}
		baseChannel.AddToQueue(netPacket2);
	}

	public void Disconnect(byte[] data)
	{
		NetManager.DisconnectPeer(this, data);
	}

	public void Disconnect(NetDataWriter writer)
	{
		NetManager.DisconnectPeer(this, writer);
	}

	public void Disconnect(byte[] data, int start, int count)
	{
		NetManager.DisconnectPeer(this, data, start, count);
	}

	public void Disconnect()
	{
		NetManager.DisconnectPeer(this);
	}

	internal DisconnectResult ProcessDisconnect(NetPacket packet)
	{
		if ((_connectionState == ConnectionState.Connected || _connectionState == ConnectionState.Outgoing) && packet.Size >= 9 && BitConverter.ToInt64(packet.RawData, 1) == _connectTime && packet.ConnectionNumber == _connectNum)
		{
			if (_connectionState != ConnectionState.Connected)
			{
				return DisconnectResult.Reject;
			}
			return DisconnectResult.Disconnect;
		}
		return DisconnectResult.None;
	}

	internal void AddToReliableChannelSendQueue(BaseChannel channel)
	{
		_channelSendQueue.Enqueue(channel);
	}

	internal ShutdownResult Shutdown(byte[] data, int start, int length, bool force)
	{
		lock (_shutdownLock)
		{
			if (_connectionState == ConnectionState.Disconnected || _connectionState == ConnectionState.ShutdownRequested)
			{
				return ShutdownResult.None;
			}
			ShutdownResult result = ((_connectionState != ConnectionState.Connected) ? ShutdownResult.Success : ShutdownResult.WasConnected);
			if (force)
			{
				_connectionState = ConnectionState.Disconnected;
				return result;
			}
			Interlocked.Exchange(ref _timeSinceLastPacket, 0);
			_shutdownPacket = new NetPacket(PacketProperty.Disconnect, length)
			{
				ConnectionNumber = _connectNum
			};
			FastBitConverter.GetBytes(_shutdownPacket.RawData, 1, _connectTime);
			if (_shutdownPacket.Size >= _mtu)
			{
				NetDebug.WriteError("[Peer] Disconnect additional data size more than MTU - 8!");
			}
			else if (data != null && length > 0)
			{
				Buffer.BlockCopy(data, start, _shutdownPacket.RawData, 9, length);
			}
			_connectionState = ConnectionState.ShutdownRequested;
			NetManager.SendRaw(_shutdownPacket, _remoteEndPoint);
			return result;
		}
	}

	private void UpdateRoundTripTime(int roundTripTime)
	{
		_rtt += roundTripTime;
		_rttCount++;
		_avgRtt = _rtt / _rttCount;
		_resendDelay = 25.0 + (double)_avgRtt * 2.1;
	}

	internal void AddReliablePacket(DeliveryMethod method, NetPacket p)
	{
		if (p.IsFragmented)
		{
			ushort fragmentId = p.FragmentId;
			byte channelId = p.ChannelId;
			if (!_holdedFragments.TryGetValue(fragmentId, out var value))
			{
				value = new IncomingFragments
				{
					Fragments = new NetPacket[p.FragmentsTotal],
					ChannelId = p.ChannelId
				};
				_holdedFragments.Add(fragmentId, value);
			}
			NetPacket[] fragments = value.Fragments;
			if (p.FragmentPart >= fragments.Length || fragments[p.FragmentPart] != null || p.ChannelId != value.ChannelId)
			{
				NetManager.PoolRecycle(p);
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
			NetPacket netPacket = NetManager.PoolGetPacket(value.TotalSize);
			int num = 0;
			for (int i = 0; i < value.ReceivedCount; i++)
			{
				NetPacket netPacket2 = fragments[i];
				int num2 = netPacket2.Size - 10;
				if (num + num2 > netPacket.RawData.Length)
				{
					_holdedFragments.Remove(fragmentId);
					NetDebug.WriteError($"Fragment error pos: {num + num2} >= resultPacketSize: {netPacket.RawData.Length} , totalSize: {value.TotalSize}");
					return;
				}
				if (netPacket2.Size > netPacket2.RawData.Length)
				{
					_holdedFragments.Remove(fragmentId);
					NetDebug.WriteError($"Fragment error size: {netPacket2.Size} > fragment.RawData.Length: {netPacket2.RawData.Length}");
					return;
				}
				Buffer.BlockCopy(netPacket2.RawData, 10, netPacket.RawData, num, num2);
				num += num2;
				NetManager.PoolRecycle(netPacket2);
				fragments[i] = null;
			}
			_holdedFragments.Remove(fragmentId);
			NetManager.CreateReceiveEvent(netPacket, method, (byte)(channelId / 4), 0, this);
		}
		else
		{
			NetManager.CreateReceiveEvent(p, method, (byte)(p.ChannelId / 4), 4, this);
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
			_mtuCheckAttempts = 0;
			packet.Property = PacketProperty.MtuOk;
			NetManager.SendRawAndRecycle(packet, _remoteEndPoint);
		}
		else if (num > _mtu && !_finishMtu && num == NetConstants.PossibleMtu[_mtuIdx + 1] - NetManager.ExtraPacketSizeForLayer)
		{
			lock (_mtuMutex)
			{
				SetMtu(_mtuIdx + 1);
			}
			if (_mtuIdx == NetConstants.PossibleMtu.Length - 1)
			{
				_finishMtu = true;
			}
			NetManager.PoolRecycle(packet);
		}
	}

	private void UpdateMtuLogic(int deltaTime)
	{
		if (_finishMtu)
		{
			return;
		}
		_mtuCheckTimer += deltaTime;
		if (_mtuCheckTimer < 1000)
		{
			return;
		}
		_mtuCheckTimer = 0;
		_mtuCheckAttempts++;
		if (_mtuCheckAttempts >= 4)
		{
			_finishMtu = true;
			return;
		}
		lock (_mtuMutex)
		{
			if (_mtuIdx < NetConstants.PossibleMtu.Length - 1)
			{
				int num = NetConstants.PossibleMtu[_mtuIdx + 1] - NetManager.ExtraPacketSizeForLayer;
				NetPacket netPacket = NetManager.PoolGetPacket(num);
				netPacket.Property = PacketProperty.MtuCheck;
				FastBitConverter.GetBytes(netPacket.RawData, 1, num);
				FastBitConverter.GetBytes(netPacket.RawData, netPacket.Size - 4, num);
				if (NetManager.SendRawAndRecycle(netPacket, _remoteEndPoint) <= 0)
				{
					_finishMtu = true;
				}
			}
		}
	}

	internal ConnectRequestResult ProcessConnectRequest(NetConnectRequestPacket connRequest)
	{
		switch (_connectionState)
		{
		case ConnectionState.Outgoing:
		{
			if (connRequest.ConnectionTime < _connectTime)
			{
				return ConnectRequestResult.P2PLose;
			}
			if (connRequest.ConnectionTime != _connectTime)
			{
				break;
			}
			SocketAddress socketAddress = _remoteEndPoint.Serialize();
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
			if (connRequest.ConnectionTime == _connectTime)
			{
				NetManager.SendRaw(_connectAcceptPacket, _remoteEndPoint);
			}
			else if (connRequest.ConnectionTime > _connectTime)
			{
				return ConnectRequestResult.Reconnection;
			}
			break;
		case ConnectionState.ShutdownRequested:
		case ConnectionState.Disconnected:
			if (connRequest.ConnectionTime >= _connectTime)
			{
				return ConnectRequestResult.NewConnection;
			}
			break;
		}
		return ConnectRequestResult.None;
	}

	internal void ProcessPacket(NetPacket packet)
	{
		if (_connectionState == ConnectionState.Outgoing || _connectionState == ConnectionState.Disconnected)
		{
			NetManager.PoolRecycle(packet);
			return;
		}
		if (packet.Property == PacketProperty.ShutdownOk)
		{
			if (_connectionState == ConnectionState.ShutdownRequested)
			{
				_connectionState = ConnectionState.Disconnected;
			}
			NetManager.PoolRecycle(packet);
			return;
		}
		if (packet.ConnectionNumber != _connectNum)
		{
			NetManager.PoolRecycle(packet);
			return;
		}
		Interlocked.Exchange(ref _timeSinceLastPacket, 0);
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
				NetPacket netPacket = NetManager.PoolGetPacket(num3);
				Buffer.BlockCopy(packet.RawData, num2, netPacket.RawData, 0, num3);
				netPacket.Size = num3;
				if (!netPacket.Verify())
				{
					break;
				}
				num2 += num3;
				ProcessPacket(netPacket);
			}
			NetManager.PoolRecycle(packet);
			break;
		}
		case PacketProperty.Ping:
			if (NetUtils.RelativeSequenceNumber(packet.Sequence, _pongPacket.Sequence) > 0)
			{
				FastBitConverter.GetBytes(_pongPacket.RawData, 3, DateTime.UtcNow.Ticks);
				_pongPacket.Sequence = packet.Sequence;
				NetManager.SendRaw(_pongPacket, _remoteEndPoint);
			}
			NetManager.PoolRecycle(packet);
			break;
		case PacketProperty.Pong:
			if (packet.Sequence == _pingPacket.Sequence)
			{
				_pingTimer.Stop();
				int num = (int)_pingTimer.ElapsedMilliseconds;
				_remoteDelta = BitConverter.ToInt64(packet.RawData, 3) + (long)num * 10000L / 2 - DateTime.UtcNow.Ticks;
				UpdateRoundTripTime(num);
				NetManager.ConnectionLatencyUpdated(this, num / 2);
			}
			NetManager.PoolRecycle(packet);
			break;
		case PacketProperty.Channeled:
		case PacketProperty.Ack:
		{
			if (packet.ChannelId > _channels.Length)
			{
				NetManager.PoolRecycle(packet);
				break;
			}
			BaseChannel baseChannel = _channels[packet.ChannelId] ?? ((packet.Property == PacketProperty.Ack) ? null : CreateChannel(packet.ChannelId));
			if (baseChannel != null && !baseChannel.ProcessPacket(packet))
			{
				NetManager.PoolRecycle(packet);
			}
			break;
		}
		case PacketProperty.Unreliable:
			NetManager.CreateReceiveEvent(packet, DeliveryMethod.Unreliable, 0, 1, this);
			break;
		case PacketProperty.MtuCheck:
		case PacketProperty.MtuOk:
			ProcessMtuPacket(packet);
			break;
		default:
			NetDebug.WriteError("Error! Unexpected packet type: " + packet.Property);
			break;
		}
	}

	private void SendMerged()
	{
		if (_mergeCount != 0)
		{
			int num = ((_mergeCount <= 1) ? NetManager.SendRaw(_mergeData.RawData, 3, _mergePos - 2, _remoteEndPoint) : NetManager.SendRaw(_mergeData.RawData, 0, 1 + _mergePos, _remoteEndPoint));
			if (NetManager.EnableStatistics)
			{
				Statistics.IncrementPacketsSent();
				Statistics.AddBytesSent(num);
			}
			_mergePos = 0;
			_mergeCount = 0;
		}
	}

	internal void SendUserData(NetPacket packet)
	{
		packet.ConnectionNumber = _connectNum;
		int num = 1 + packet.Size + 2;
		if (num + 20 >= _mtu)
		{
			int num2 = NetManager.SendRaw(packet, _remoteEndPoint);
			if (NetManager.EnableStatistics)
			{
				Statistics.IncrementPacketsSent();
				Statistics.AddBytesSent(num2);
			}
			return;
		}
		if (_mergePos + num > _mtu)
		{
			SendMerged();
		}
		FastBitConverter.GetBytes(_mergeData.RawData, _mergePos + 1, (ushort)packet.Size);
		Buffer.BlockCopy(packet.RawData, 0, _mergeData.RawData, _mergePos + 1 + 2, packet.Size);
		_mergePos += packet.Size + 2;
		_mergeCount++;
	}

	internal void Update(int deltaTime)
	{
		Interlocked.Add(ref _timeSinceLastPacket, deltaTime);
		switch (_connectionState)
		{
		case ConnectionState.Connected:
			if (_timeSinceLastPacket > NetManager.DisconnectTimeout)
			{
				NetManager.DisconnectPeerForce(this, DisconnectReason.Timeout, SocketError.Success, null);
				return;
			}
			break;
		case ConnectionState.ShutdownRequested:
			if (_timeSinceLastPacket > NetManager.DisconnectTimeout)
			{
				_connectionState = ConnectionState.Disconnected;
				return;
			}
			_shutdownTimer += deltaTime;
			if (_shutdownTimer >= 300)
			{
				_shutdownTimer = 0;
				NetManager.SendRaw(_shutdownPacket, _remoteEndPoint);
			}
			return;
		case ConnectionState.Outgoing:
			_connectTimer += deltaTime;
			if (_connectTimer > NetManager.ReconnectDelay)
			{
				_connectTimer = 0;
				_connectAttempts++;
				if (_connectAttempts > NetManager.MaxConnectAttempts)
				{
					NetManager.DisconnectPeerForce(this, DisconnectReason.ConnectionFailed, SocketError.Success, null);
				}
				else
				{
					NetManager.SendRaw(_connectRequestPacket, _remoteEndPoint);
				}
			}
			return;
		case ConnectionState.Disconnected:
			return;
		}
		_pingSendTimer += deltaTime;
		if (_pingSendTimer >= NetManager.PingInterval)
		{
			_pingSendTimer = 0;
			_pingPacket.Sequence++;
			if (_pingTimer.IsRunning)
			{
				UpdateRoundTripTime((int)_pingTimer.ElapsedMilliseconds);
			}
			_pingTimer.Restart();
			NetManager.SendRaw(_pingPacket, _remoteEndPoint);
		}
		_rttResetTimer += deltaTime;
		if (_rttResetTimer >= NetManager.PingInterval * 3)
		{
			_rttResetTimer = 0;
			_rtt = _avgRtt;
			_rttCount = 1;
		}
		UpdateMtuLogic(deltaTime);
		int count = _channelSendQueue.Count;
		BaseChannel result;
		while (count-- > 0 && _channelSendQueue.TryDequeue(out result))
		{
			if (result.SendAndCheckQueue())
			{
				_channelSendQueue.Enqueue(result);
			}
		}
		lock (_unreliableChannel)
		{
			int count2 = _unreliableChannel.Count;
			for (int i = 0; i < count2; i++)
			{
				NetPacket packet = _unreliableChannel.Dequeue();
				SendUserData(packet);
				NetManager.PoolRecycle(packet);
			}
		}
		SendMerged();
	}

	internal void RecycleAndDeliver(NetPacket packet)
	{
		if (packet.UserData != null)
		{
			if (packet.IsFragmented)
			{
				_deliveredFragments.TryGetValue(packet.FragmentId, out var value);
				value++;
				if (value == packet.FragmentsTotal)
				{
					NetManager.MessageDelivered(this, packet.UserData);
					_deliveredFragments.Remove(packet.FragmentId);
				}
				else
				{
					_deliveredFragments[packet.FragmentId] = value;
				}
			}
			else
			{
				NetManager.MessageDelivered(this, packet.UserData);
			}
			packet.UserData = null;
		}
		NetManager.PoolRecycle(packet);
	}
}

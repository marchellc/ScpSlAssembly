using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using LiteNetLib.Layers;
using LiteNetLib.Utils;
using Steam;

namespace LiteNetLib;

public class NetManager : IEnumerable<NetPeer>, IEnumerable
{
	private class IPEndPointComparer : IEqualityComparer<IPEndPoint>
	{
		public bool Equals(IPEndPoint x, IPEndPoint y)
		{
			if (x.Address.Equals(y.Address))
			{
				return x.Port == y.Port;
			}
			return false;
		}

		public int GetHashCode(IPEndPoint obj)
		{
			return obj.GetHashCode();
		}
	}

	public struct NetPeerEnumerator : IEnumerator<NetPeer>, IEnumerator, IDisposable
	{
		private readonly NetPeer _initialPeer;

		private NetPeer _p;

		public NetPeer Current => _p;

		object IEnumerator.Current => _p;

		public NetPeerEnumerator(NetPeer p)
		{
			_initialPeer = p;
			_p = null;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			_p = ((_p == null) ? _initialPeer : _p.NextPeer);
			return _p != null;
		}

		public void Reset()
		{
			throw new NotSupportedException();
		}
	}

	private Thread _logicThread;

	private bool _manualMode;

	private readonly AutoResetEvent _updateTriggerEvent = new AutoResetEvent(initialState: true);

	private NetEvent _pendingEventHead;

	private NetEvent _pendingEventTail;

	private NetEvent _netEventPoolHead;

	private readonly INetEventListener _netEventListener;

	private readonly IDeliveryEventListener _deliveryEventListener;

	private readonly INtpEventListener _ntpEventListener;

	private readonly IPeerAddressChangedListener _peerAddressChangedListener;

	private readonly Dictionary<IPEndPoint, NetPeer> _peersDict = new Dictionary<IPEndPoint, NetPeer>(new IPEndPointComparer());

	private readonly Dictionary<IPEndPoint, ConnectionRequest> _requestsDict = new Dictionary<IPEndPoint, ConnectionRequest>(new IPEndPointComparer());

	private readonly Dictionary<IPEndPoint, NtpRequest> _ntpRequests = new Dictionary<IPEndPoint, NtpRequest>(new IPEndPointComparer());

	private readonly ReaderWriterLockSlim _peersLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

	private volatile NetPeer _headPeer;

	private int _connectedPeersCount;

	private readonly List<NetPeer> _connectedPeerListCache = new List<NetPeer>();

	private NetPeer[] _peersArray = new NetPeer[32];

	private readonly PacketLayerBase _extraPacketLayer;

	private int _lastPeerId;

	private ConcurrentQueue<int> _peerIds = new ConcurrentQueue<int>();

	private byte _channelsCount = 1;

	private readonly object _eventLock = new object();

	public bool UnconnectedMessagesEnabled;

	public bool NatPunchEnabled;

	public int UpdateTime = 15;

	public int PingInterval = 1000;

	public int DisconnectTimeout = 5000;

	public bool SimulatePacketLoss;

	public bool SimulateLatency;

	public int SimulationPacketLossChance = 10;

	public int SimulationMinLatency = 30;

	public int SimulationMaxLatency = 100;

	public bool UnsyncedEvents;

	public bool UnsyncedReceiveEvent;

	public bool UnsyncedDeliveryEvent;

	public bool BroadcastReceiveEnabled;

	public int ReconnectDelay = 500;

	public int MaxConnectAttempts = 10;

	public bool ReuseAddress;

	public readonly NetStatistics Statistics = new NetStatistics();

	public bool EnableStatistics;

	public readonly NatPunchModule NatPunchModule;

	public bool AutoRecycle;

	public bool IPv6Enabled = true;

	public int MtuOverride;

	public bool UseSafeMtu;

	public bool UseNativeSockets;

	public bool DisconnectOnUnreachable;

	public bool AllowPeerAddressChange;

	private NetPacket _poolHead;

	private int _poolCount;

	private readonly object _poolLock = new object();

	public int PacketPoolSize = 1000;

	private const int ReceivePollingTime = 500000;

	private Socket _udpSocketv4;

	private Socket _udpSocketv6;

	private Thread _receiveThread;

	private IPEndPoint _bufferEndPointv4;

	private IPEndPoint _bufferEndPointv6;

	private PausedSocketFix _pausedSocketFix;

	[ThreadStatic]
	private static byte[] _sendToBuffer;

	[ThreadStatic]
	private static byte[] _endPointBuffer;

	private readonly Dictionary<NativeAddr, IPEndPoint> _nativeAddrMap = new Dictionary<NativeAddr, IPEndPoint>();

	private const int SioUdpConnreset = -1744830452;

	private static readonly IPAddress MulticastAddressV6;

	public static readonly bool IPv6Support;

	public int MaxPacketsReceivePerUpdate;

	internal bool NotConnected;

	public bool IsRunning { get; private set; }

	public int LocalPort { get; private set; }

	public NetPeer FirstPeer => _headPeer;

	public byte ChannelsCount
	{
		get
		{
			return _channelsCount;
		}
		set
		{
			if (value < 1 || value > 64)
			{
				throw new ArgumentException("Channels count must be between 1 and 64");
			}
			_channelsCount = value;
		}
	}

	public List<NetPeer> ConnectedPeerList
	{
		get
		{
			GetPeersNonAlloc(_connectedPeerListCache, ConnectionState.Connected);
			return _connectedPeerListCache;
		}
	}

	public int ConnectedPeersCount => Interlocked.CompareExchange(ref _connectedPeersCount, 0, 0);

	public int ExtraPacketSizeForLayer => _extraPacketLayer?.ExtraPacketSizeForLayer ?? 0;

	public int PoolCount => _poolCount;

	public short Ttl
	{
		get
		{
			return _udpSocketv4.Ttl;
		}
		internal set
		{
			_udpSocketv4.Ttl = value;
		}
	}

	public NetPeer GetPeerById(int id)
	{
		if (id >= 0 && id < _peersArray.Length)
		{
			return _peersArray[id];
		}
		return null;
	}

	public bool TryGetPeerById(int id, out NetPeer peer)
	{
		peer = GetPeerById(id);
		return peer != null;
	}

	private bool TryGetPeer(IPEndPoint endPoint, out NetPeer peer)
	{
		_peersLock.EnterReadLock();
		bool result = _peersDict.TryGetValue(endPoint, out peer);
		_peersLock.ExitReadLock();
		return result;
	}

	private void AddPeer(NetPeer peer)
	{
		_peersLock.EnterWriteLock();
		if (_headPeer != null)
		{
			peer.NextPeer = _headPeer;
			_headPeer.PrevPeer = peer;
		}
		_headPeer = peer;
		_peersDict.Add(peer.EndPoint, peer);
		if (peer.Id >= _peersArray.Length)
		{
			int num = _peersArray.Length * 2;
			while (peer.Id >= num)
			{
				num *= 2;
			}
			Array.Resize(ref _peersArray, num);
		}
		_peersArray[peer.Id] = peer;
		RegisterEndPoint(peer.EndPoint);
		_peersLock.ExitWriteLock();
	}

	private void RemovePeer(NetPeer peer)
	{
		_peersLock.EnterWriteLock();
		RemovePeerInternal(peer);
		_peersLock.ExitWriteLock();
	}

	private void RemovePeerInternal(NetPeer peer)
	{
		if (_peersDict.Remove(peer.EndPoint))
		{
			if (peer == _headPeer)
			{
				_headPeer = peer.NextPeer;
			}
			if (peer.PrevPeer != null)
			{
				peer.PrevPeer.NextPeer = peer.NextPeer;
			}
			if (peer.NextPeer != null)
			{
				peer.NextPeer.PrevPeer = peer.PrevPeer;
			}
			peer.PrevPeer = null;
			_peersArray[peer.Id] = null;
			_peerIds.Enqueue(peer.Id);
			UnregisterEndPoint(peer.EndPoint);
		}
	}

	public NetManager(INetEventListener listener, PacketLayerBase extraPacketLayer = null)
	{
		_netEventListener = listener;
		_deliveryEventListener = listener as IDeliveryEventListener;
		_ntpEventListener = listener as INtpEventListener;
		_peerAddressChangedListener = listener as IPeerAddressChangedListener;
		NatPunchModule = new NatPunchModule(this);
		_extraPacketLayer = extraPacketLayer;
	}

	internal void ConnectionLatencyUpdated(NetPeer fromPeer, int latency)
	{
		CreateEvent(NetEvent.EType.ConnectionLatencyUpdated, fromPeer, null, SocketError.Success, latency, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
	}

	internal void MessageDelivered(NetPeer fromPeer, object userData)
	{
		if (_deliveryEventListener != null)
		{
			CreateEvent(NetEvent.EType.MessageDelivered, fromPeer, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, null, userData);
		}
	}

	internal void DisconnectPeerForce(NetPeer peer, DisconnectReason reason, SocketError socketErrorCode, NetPacket eventData)
	{
		DisconnectPeer(peer, reason, socketErrorCode, force: true, null, 0, 0, eventData);
	}

	private void DisconnectPeer(NetPeer peer, DisconnectReason reason, SocketError socketErrorCode, bool force, byte[] data, int start, int count, NetPacket eventData)
	{
		switch (peer.Shutdown(data, start, count, force))
		{
		case ShutdownResult.None:
			return;
		case ShutdownResult.WasConnected:
			Interlocked.Decrement(ref _connectedPeersCount);
			break;
		}
		CreateEvent(NetEvent.EType.Disconnect, peer, null, socketErrorCode, 0, reason, null, DeliveryMethod.Unreliable, 0, eventData);
	}

	private void CreateEvent(NetEvent.EType type, NetPeer peer = null, IPEndPoint remoteEndPoint = null, SocketError errorCode = SocketError.Success, int latency = 0, DisconnectReason disconnectReason = DisconnectReason.ConnectionFailed, ConnectionRequest connectionRequest = null, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, byte channelNumber = 0, NetPacket readerSource = null, object userData = null)
	{
		bool flag = UnsyncedEvents;
		switch (type)
		{
		case NetEvent.EType.Connect:
			Interlocked.Increment(ref _connectedPeersCount);
			break;
		case NetEvent.EType.MessageDelivered:
			flag = UnsyncedDeliveryEvent;
			break;
		}
		NetEvent netEvent;
		lock (_eventLock)
		{
			netEvent = _netEventPoolHead;
			if (netEvent == null)
			{
				netEvent = new NetEvent(this);
			}
			else
			{
				_netEventPoolHead = netEvent.Next;
			}
		}
		netEvent.Next = null;
		netEvent.Type = type;
		netEvent.DataReader.SetSource(readerSource, readerSource?.GetHeaderSize() ?? 0);
		netEvent.Peer = peer;
		netEvent.RemoteEndPoint = remoteEndPoint;
		netEvent.Latency = latency;
		netEvent.ErrorCode = errorCode;
		netEvent.DisconnectReason = disconnectReason;
		netEvent.ConnectionRequest = connectionRequest;
		netEvent.DeliveryMethod = deliveryMethod;
		netEvent.ChannelNumber = channelNumber;
		netEvent.UserData = userData;
		if (flag || _manualMode)
		{
			ProcessEvent(netEvent);
			return;
		}
		lock (_eventLock)
		{
			if (_pendingEventTail == null)
			{
				_pendingEventHead = netEvent;
			}
			else
			{
				_pendingEventTail.Next = netEvent;
			}
			_pendingEventTail = netEvent;
		}
	}

	private void ProcessEvent(NetEvent evt)
	{
		bool isNull = evt.DataReader.IsNull;
		switch (evt.Type)
		{
		case NetEvent.EType.Connect:
			_netEventListener.OnPeerConnected(evt.Peer);
			break;
		case NetEvent.EType.Disconnect:
		{
			DisconnectInfo disconnectInfo = default(DisconnectInfo);
			disconnectInfo.Reason = evt.DisconnectReason;
			disconnectInfo.AdditionalData = evt.DataReader;
			disconnectInfo.SocketErrorCode = evt.ErrorCode;
			DisconnectInfo disconnectInfo2 = disconnectInfo;
			_netEventListener.OnPeerDisconnected(evt.Peer, disconnectInfo2);
			break;
		}
		case NetEvent.EType.Receive:
			_netEventListener.OnNetworkReceive(evt.Peer, evt.DataReader, evt.ChannelNumber, evt.DeliveryMethod);
			break;
		case NetEvent.EType.ReceiveUnconnected:
			_netEventListener.OnNetworkReceiveUnconnected(evt.RemoteEndPoint, evt.DataReader, UnconnectedMessageType.BasicMessage);
			break;
		case NetEvent.EType.Broadcast:
			_netEventListener.OnNetworkReceiveUnconnected(evt.RemoteEndPoint, evt.DataReader, UnconnectedMessageType.Broadcast);
			break;
		case NetEvent.EType.Error:
			_netEventListener.OnNetworkError(evt.RemoteEndPoint, evt.ErrorCode);
			break;
		case NetEvent.EType.ConnectionLatencyUpdated:
			_netEventListener.OnNetworkLatencyUpdate(evt.Peer, evt.Latency);
			break;
		case NetEvent.EType.ConnectionRequest:
			_netEventListener.OnConnectionRequest(evt.ConnectionRequest);
			break;
		case NetEvent.EType.MessageDelivered:
			_deliveryEventListener.OnMessageDelivered(evt.Peer, evt.UserData);
			break;
		case NetEvent.EType.PeerAddressChanged:
		{
			_peersLock.EnterUpgradeableReadLock();
			IPEndPoint iPEndPoint = null;
			if (_peersDict.ContainsKey(evt.Peer.EndPoint))
			{
				_peersLock.EnterWriteLock();
				_peersDict.Remove(evt.Peer.EndPoint);
				iPEndPoint = evt.Peer.EndPoint;
				evt.Peer.FinishEndPointChange(evt.RemoteEndPoint);
				_peersDict.Add(evt.Peer.EndPoint, evt.Peer);
				_peersLock.ExitWriteLock();
			}
			_peersLock.ExitUpgradeableReadLock();
			if (iPEndPoint != null)
			{
				_peerAddressChangedListener.OnPeerAddressChanged(evt.Peer, iPEndPoint);
			}
			break;
		}
		}
		if (isNull)
		{
			RecycleEvent(evt);
		}
		else if (AutoRecycle)
		{
			evt.DataReader.RecycleInternal();
		}
	}

	internal void RecycleEvent(NetEvent evt)
	{
		evt.Peer = null;
		evt.ErrorCode = SocketError.Success;
		evt.RemoteEndPoint = null;
		evt.ConnectionRequest = null;
		lock (_eventLock)
		{
			evt.Next = _netEventPoolHead;
			_netEventPoolHead = evt;
		}
	}

	private void UpdateLogic()
	{
		List<NetPeer> list = new List<NetPeer>();
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		while (IsRunning)
		{
			try
			{
				int num = (int)stopwatch.ElapsedMilliseconds;
				num = ((num <= 0) ? 1 : num);
				stopwatch.Restart();
				for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
				{
					if (netPeer.ConnectionState == ConnectionState.Disconnected && netPeer.TimeSinceLastPacket > DisconnectTimeout)
					{
						list.Add(netPeer);
					}
					else
					{
						netPeer.Update(num);
					}
				}
				if (list.Count > 0)
				{
					_peersLock.EnterWriteLock();
					for (int i = 0; i < list.Count; i++)
					{
						RemovePeerInternal(list[i]);
					}
					_peersLock.ExitWriteLock();
					list.Clear();
				}
				ProcessNtpRequests(num);
				int num2 = UpdateTime - (int)stopwatch.ElapsedMilliseconds;
				if (num2 > 0)
				{
					_updateTriggerEvent.WaitOne(num2);
				}
			}
			catch (ThreadAbortException)
			{
				return;
			}
			catch (Exception ex2)
			{
				NetDebug.WriteError("[NM] LogicThread error: " + ex2);
			}
		}
		stopwatch.Stop();
	}

	[Conditional("DEBUG")]
	private void ProcessDelayedPackets()
	{
	}

	private void ProcessNtpRequests(int elapsedMilliseconds)
	{
		List<IPEndPoint> list = null;
		foreach (KeyValuePair<IPEndPoint, NtpRequest> ntpRequest in _ntpRequests)
		{
			ntpRequest.Value.Send(_udpSocketv4, elapsedMilliseconds);
			if (ntpRequest.Value.NeedToKill)
			{
				if (list == null)
				{
					list = new List<IPEndPoint>();
				}
				list.Add(ntpRequest.Key);
			}
		}
		if (list == null)
		{
			return;
		}
		foreach (IPEndPoint item in list)
		{
			_ntpRequests.Remove(item);
		}
	}

	public void ManualUpdate(int elapsedMilliseconds)
	{
		if (!_manualMode)
		{
			return;
		}
		for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if (netPeer.ConnectionState == ConnectionState.Disconnected && netPeer.TimeSinceLastPacket > DisconnectTimeout)
			{
				RemovePeerInternal(netPeer);
			}
			else
			{
				netPeer.Update(elapsedMilliseconds);
			}
		}
		ProcessNtpRequests(elapsedMilliseconds);
	}

	internal NetPeer OnConnectionSolved(ConnectionRequest request, byte[] rejectData, int start, int length)
	{
		NetPeer value = null;
		if (request.Result == ConnectionRequestResult.RejectForce)
		{
			if (rejectData != null && length > 0)
			{
				NetPacket netPacket = PoolGetWithProperty(PacketProperty.Disconnect, length);
				netPacket.ConnectionNumber = request.InternalPacket.ConnectionNumber;
				FastBitConverter.GetBytes(netPacket.RawData, 1, request.InternalPacket.ConnectionTime);
				if (netPacket.Size >= NetConstants.PossibleMtu[0])
				{
					NetDebug.WriteError("[Peer] Disconnect additional data size more than MTU!");
				}
				else
				{
					Buffer.BlockCopy(rejectData, start, netPacket.RawData, 9, length);
				}
				SendRawAndRecycle(netPacket, request.RemoteEndPoint);
			}
		}
		else
		{
			_peersLock.EnterUpgradeableReadLock();
			if (_peersDict.TryGetValue(request.RemoteEndPoint, out value))
			{
				_peersLock.ExitUpgradeableReadLock();
			}
			else if (request.Result == ConnectionRequestResult.Reject)
			{
				value = new NetPeer(this, request.RemoteEndPoint, GetNextPeerId());
				value.Reject(request.InternalPacket, rejectData, start, length);
				AddPeer(value);
				_peersLock.ExitUpgradeableReadLock();
			}
			else
			{
				value = new NetPeer(this, request, GetNextPeerId());
				AddPeer(value);
				_peersLock.ExitUpgradeableReadLock();
				CreateEvent(NetEvent.EType.Connect, value, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
			}
		}
		lock (_requestsDict)
		{
			_requestsDict.Remove(request.RemoteEndPoint);
			return value;
		}
	}

	private int GetNextPeerId()
	{
		if (!_peerIds.TryDequeue(out var result))
		{
			return _lastPeerId++;
		}
		return result;
	}

	private void ProcessConnectRequest(IPEndPoint remoteEndPoint, NetPeer netPeer, NetConnectRequestPacket connRequest)
	{
		if (netPeer != null)
		{
			ConnectRequestResult connectRequestResult = netPeer.ProcessConnectRequest(connRequest);
			switch (connectRequestResult)
			{
			default:
				return;
			case ConnectRequestResult.Reconnection:
				DisconnectPeerForce(netPeer, DisconnectReason.Reconnect, SocketError.Success, null);
				RemovePeer(netPeer);
				break;
			case ConnectRequestResult.NewConnection:
				RemovePeer(netPeer);
				break;
			case ConnectRequestResult.P2PLose:
				DisconnectPeerForce(netPeer, DisconnectReason.PeerToPeerConnection, SocketError.Success, null);
				RemovePeer(netPeer);
				break;
			}
			if (connectRequestResult != ConnectRequestResult.P2PLose)
			{
				connRequest.ConnectionNumber = (byte)((netPeer.ConnectionNum + 1) % 4);
			}
		}
		ConnectionRequest value;
		lock (_requestsDict)
		{
			if (_requestsDict.TryGetValue(remoteEndPoint, out value))
			{
				value.UpdateRequest(connRequest);
				return;
			}
			value = new ConnectionRequest(remoteEndPoint, connRequest, this);
			_requestsDict.Add(remoteEndPoint, value);
		}
		CreateEvent(NetEvent.EType.ConnectionRequest, null, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, value, DeliveryMethod.Unreliable, 0);
	}

	private void OnMessageReceived(NetPacket packet, IPEndPoint remoteEndPoint)
	{
		int size = packet.Size;
		if (EnableStatistics)
		{
			Statistics.IncrementPacketsReceived();
			Statistics.AddBytesReceived(size);
		}
		if (_ntpRequests.Count > 0 && _ntpRequests.TryGetValue(remoteEndPoint, out var _))
		{
			if (packet.Size >= 48)
			{
				byte[] array = new byte[packet.Size];
				Buffer.BlockCopy(packet.RawData, 0, array, 0, packet.Size);
				NtpPacket ntpPacket = NtpPacket.FromServerResponse(array, DateTime.UtcNow);
				try
				{
					ntpPacket.ValidateReply();
				}
				catch (InvalidOperationException)
				{
					ntpPacket = null;
				}
				if (ntpPacket != null)
				{
					_ntpRequests.Remove(remoteEndPoint);
					_ntpEventListener?.OnNtpResponse(ntpPacket);
				}
			}
			return;
		}
		if (_extraPacketLayer != null)
		{
			int offset = 0;
			_extraPacketLayer.ProcessInboundPacket(ref remoteEndPoint, ref packet.RawData, ref offset, ref packet.Size);
			if (packet.Size == 0)
			{
				return;
			}
		}
		if (!packet.Verify())
		{
			if (packet.RawData.Length >= 5 && packet.RawData[4] == 84)
			{
				_udpSocketv4.SendTo(SteamServerInfo.Serialize(), SocketFlags.None, remoteEndPoint);
				PoolRecycle(packet);
			}
			else
			{
				NetDebug.WriteError("[NM] DataReceived: bad!");
				PoolRecycle(packet);
			}
			return;
		}
		switch (packet.Property)
		{
		case PacketProperty.ConnectRequest:
			if (NetConnectRequestPacket.GetProtocolId(packet) != 13)
			{
				SendRawAndRecycle(PoolGetWithProperty(PacketProperty.InvalidProtocol), remoteEndPoint);
				return;
			}
			break;
		case PacketProperty.Broadcast:
			if (BroadcastReceiveEnabled)
			{
				CreateEvent(NetEvent.EType.Broadcast, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
			}
			return;
		case PacketProperty.UnconnectedMessage:
			if (UnconnectedMessagesEnabled)
			{
				CreateEvent(NetEvent.EType.ReceiveUnconnected, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
			}
			return;
		case PacketProperty.NatMessage:
			if (NatPunchEnabled)
			{
				NatPunchModule.ProcessMessage(remoteEndPoint, packet);
			}
			return;
		}
		_peersLock.EnterReadLock();
		NetPeer value2;
		bool flag = _peersDict.TryGetValue(remoteEndPoint, out value2);
		_peersLock.ExitReadLock();
		if (flag && EnableStatistics)
		{
			value2.Statistics.IncrementPacketsReceived();
			value2.Statistics.AddBytesReceived(size);
		}
		switch (packet.Property)
		{
		case PacketProperty.ConnectRequest:
		{
			NetConnectRequestPacket netConnectRequestPacket = NetConnectRequestPacket.FromData(packet);
			if (netConnectRequestPacket != null)
			{
				ProcessConnectRequest(remoteEndPoint, value2, netConnectRequestPacket);
			}
			break;
		}
		case PacketProperty.PeerNotFound:
			if (flag)
			{
				if (value2.ConnectionState == ConnectionState.Connected)
				{
					if (packet.Size == 1)
					{
						value2.ResetMtu();
						SendRaw(NetConnectAcceptPacket.MakeNetworkChanged(value2), remoteEndPoint);
					}
					else if (packet.Size == 2 && packet.RawData[1] == 1)
					{
						DisconnectPeerForce(value2, DisconnectReason.PeerNotFound, SocketError.Success, null);
					}
				}
			}
			else
			{
				if (packet.Size <= 1)
				{
					break;
				}
				bool flag2 = false;
				if (AllowPeerAddressChange)
				{
					NetConnectAcceptPacket netConnectAcceptPacket = NetConnectAcceptPacket.FromData(packet);
					if (netConnectAcceptPacket != null && netConnectAcceptPacket.PeerNetworkChanged && netConnectAcceptPacket.PeerId < _peersArray.Length)
					{
						_peersLock.EnterUpgradeableReadLock();
						NetPeer netPeer = _peersArray[netConnectAcceptPacket.PeerId];
						if (netPeer != null && netPeer.ConnectTime == netConnectAcceptPacket.ConnectionTime && netPeer.ConnectionNum == netConnectAcceptPacket.ConnectionNumber)
						{
							if (netPeer.ConnectionState == ConnectionState.Connected)
							{
								netPeer.InitiateEndPointChange();
								if (_peerAddressChangedListener != null)
								{
									CreateEvent(NetEvent.EType.PeerAddressChanged, netPeer, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
								}
							}
							flag2 = true;
						}
						_peersLock.ExitUpgradeableReadLock();
					}
				}
				PoolRecycle(packet);
				if (!flag2)
				{
					NetPacket netPacket = PoolGetWithProperty(PacketProperty.PeerNotFound, 1);
					netPacket.RawData[1] = 1;
					SendRawAndRecycle(netPacket, remoteEndPoint);
				}
			}
			break;
		case PacketProperty.InvalidProtocol:
			if (flag && value2.ConnectionState == ConnectionState.Outgoing)
			{
				DisconnectPeerForce(value2, DisconnectReason.InvalidProtocol, SocketError.Success, null);
			}
			break;
		case PacketProperty.Disconnect:
			if (flag)
			{
				DisconnectResult disconnectResult = value2.ProcessDisconnect(packet);
				if (disconnectResult == DisconnectResult.None)
				{
					PoolRecycle(packet);
					break;
				}
				DisconnectPeerForce(value2, (disconnectResult == DisconnectResult.Disconnect) ? DisconnectReason.RemoteConnectionClose : DisconnectReason.ConnectionRejected, SocketError.Success, packet);
			}
			else
			{
				PoolRecycle(packet);
			}
			SendRawAndRecycle(PoolGetWithProperty(PacketProperty.ShutdownOk), remoteEndPoint);
			break;
		case PacketProperty.ConnectAccept:
			if (flag)
			{
				NetConnectAcceptPacket netConnectAcceptPacket2 = NetConnectAcceptPacket.FromData(packet);
				if (netConnectAcceptPacket2 != null && value2.ProcessConnectAccept(netConnectAcceptPacket2))
				{
					CreateEvent(NetEvent.EType.Connect, value2, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
				}
			}
			break;
		default:
			if (flag)
			{
				value2.ProcessPacket(packet);
			}
			else
			{
				SendRawAndRecycle(PoolGetWithProperty(PacketProperty.PeerNotFound), remoteEndPoint);
			}
			break;
		}
	}

	internal void CreateReceiveEvent(NetPacket packet, DeliveryMethod method, byte channelNumber, int headerSize, NetPeer fromPeer)
	{
		if (UnsyncedEvents || UnsyncedReceiveEvent || _manualMode)
		{
			NetEvent netEvent;
			lock (_eventLock)
			{
				netEvent = _netEventPoolHead;
				if (netEvent == null)
				{
					netEvent = new NetEvent(this);
				}
				else
				{
					_netEventPoolHead = netEvent.Next;
				}
			}
			netEvent.Next = null;
			netEvent.Type = NetEvent.EType.Receive;
			netEvent.DataReader.SetSource(packet, headerSize);
			netEvent.Peer = fromPeer;
			netEvent.DeliveryMethod = method;
			netEvent.ChannelNumber = channelNumber;
			ProcessEvent(netEvent);
			return;
		}
		lock (_eventLock)
		{
			NetEvent netEvent = _netEventPoolHead;
			if (netEvent == null)
			{
				netEvent = new NetEvent(this);
			}
			else
			{
				_netEventPoolHead = netEvent.Next;
			}
			netEvent.Next = null;
			netEvent.Type = NetEvent.EType.Receive;
			netEvent.DataReader.SetSource(packet, headerSize);
			netEvent.Peer = fromPeer;
			netEvent.DeliveryMethod = method;
			netEvent.ChannelNumber = channelNumber;
			if (_pendingEventTail == null)
			{
				_pendingEventHead = netEvent;
			}
			else
			{
				_pendingEventTail.Next = netEvent;
			}
			_pendingEventTail = netEvent;
		}
	}

	public void SendToAll(NetDataWriter writer, DeliveryMethod options)
	{
		SendToAll(writer.Data, 0, writer.Length, options);
	}

	public void SendToAll(byte[] data, DeliveryMethod options)
	{
		SendToAll(data, 0, data.Length, options);
	}

	public void SendToAll(byte[] data, int start, int length, DeliveryMethod options)
	{
		SendToAll(data, start, length, 0, options);
	}

	public void SendToAll(NetDataWriter writer, byte channelNumber, DeliveryMethod options)
	{
		SendToAll(writer.Data, 0, writer.Length, channelNumber, options);
	}

	public void SendToAll(byte[] data, byte channelNumber, DeliveryMethod options)
	{
		SendToAll(data, 0, data.Length, channelNumber, options);
	}

	public void SendToAll(byte[] data, int start, int length, byte channelNumber, DeliveryMethod options)
	{
		try
		{
			_peersLock.EnterReadLock();
			for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				netPeer.Send(data, start, length, channelNumber, options);
			}
		}
		finally
		{
			_peersLock.ExitReadLock();
		}
	}

	public void SendToAll(NetDataWriter writer, DeliveryMethod options, NetPeer excludePeer)
	{
		SendToAll(writer.Data, 0, writer.Length, 0, options, excludePeer);
	}

	public void SendToAll(byte[] data, DeliveryMethod options, NetPeer excludePeer)
	{
		SendToAll(data, 0, data.Length, 0, options, excludePeer);
	}

	public void SendToAll(byte[] data, int start, int length, DeliveryMethod options, NetPeer excludePeer)
	{
		SendToAll(data, start, length, 0, options, excludePeer);
	}

	public void SendToAll(NetDataWriter writer, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		SendToAll(writer.Data, 0, writer.Length, channelNumber, options, excludePeer);
	}

	public void SendToAll(byte[] data, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		SendToAll(data, 0, data.Length, channelNumber, options, excludePeer);
	}

	public void SendToAll(byte[] data, int start, int length, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		try
		{
			_peersLock.EnterReadLock();
			for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				if (netPeer != excludePeer)
				{
					netPeer.Send(data, start, length, channelNumber, options);
				}
			}
		}
		finally
		{
			_peersLock.ExitReadLock();
		}
	}

	public bool Start()
	{
		return Start(0);
	}

	public bool Start(IPAddress addressIPv4, IPAddress addressIPv6, int port)
	{
		return Start(addressIPv4, addressIPv6, port, manualMode: false);
	}

	public bool Start(string addressIPv4, string addressIPv6, int port)
	{
		IPAddress addressIPv7 = NetUtils.ResolveAddress(addressIPv4);
		IPAddress addressIPv8 = NetUtils.ResolveAddress(addressIPv6);
		return Start(addressIPv7, addressIPv8, port);
	}

	public bool Start(int port)
	{
		return Start(IPAddress.Any, IPAddress.IPv6Any, port);
	}

	public bool StartInManualMode(IPAddress addressIPv4, IPAddress addressIPv6, int port)
	{
		return Start(addressIPv4, addressIPv6, port, manualMode: true);
	}

	public bool StartInManualMode(string addressIPv4, string addressIPv6, int port)
	{
		IPAddress addressIPv7 = NetUtils.ResolveAddress(addressIPv4);
		IPAddress addressIPv8 = NetUtils.ResolveAddress(addressIPv6);
		return StartInManualMode(addressIPv7, addressIPv8, port);
	}

	public bool StartInManualMode(int port)
	{
		return StartInManualMode(IPAddress.Any, IPAddress.IPv6Any, port);
	}

	public bool SendUnconnectedMessage(byte[] message, IPEndPoint remoteEndPoint)
	{
		return SendUnconnectedMessage(message, 0, message.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(NetDataWriter writer, string address, int port)
	{
		IPEndPoint remoteEndPoint = NetUtils.MakeEndPoint(address, port);
		return SendUnconnectedMessage(writer.Data, 0, writer.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(NetDataWriter writer, IPEndPoint remoteEndPoint)
	{
		return SendUnconnectedMessage(writer.Data, 0, writer.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(byte[] message, int start, int length, IPEndPoint remoteEndPoint)
	{
		NetPacket packet = PoolGetWithData(PacketProperty.UnconnectedMessage, message, start, length);
		return SendRawAndRecycle(packet, remoteEndPoint) > 0;
	}

	public void TriggerUpdate()
	{
		_updateTriggerEvent.Set();
	}

	public void PollEvents()
	{
		if (_manualMode)
		{
			if (_udpSocketv4 != null)
			{
				ManualReceive(_udpSocketv4, _bufferEndPointv4);
			}
			if (_udpSocketv6 != null && _udpSocketv6 != _udpSocketv4)
			{
				ManualReceive(_udpSocketv6, _bufferEndPointv6);
			}
		}
		else if (!UnsyncedEvents)
		{
			NetEvent netEvent;
			lock (_eventLock)
			{
				netEvent = _pendingEventHead;
				_pendingEventHead = null;
				_pendingEventTail = null;
			}
			while (netEvent != null)
			{
				NetEvent next = netEvent.Next;
				ProcessEvent(netEvent);
				netEvent = next;
			}
		}
	}

	public NetPeer Connect(string address, int port, string key)
	{
		return Connect(address, port, NetDataWriter.FromString(key));
	}

	public NetPeer Connect(string address, int port, NetDataWriter connectionData)
	{
		IPEndPoint target;
		try
		{
			target = NetUtils.MakeEndPoint(address, port);
		}
		catch
		{
			CreateEvent(NetEvent.EType.Disconnect, null, null, SocketError.Success, 0, DisconnectReason.UnknownHost, null, DeliveryMethod.Unreliable, 0);
			return null;
		}
		return Connect(target, connectionData);
	}

	public NetPeer Connect(IPEndPoint target, string key)
	{
		return Connect(target, NetDataWriter.FromString(key));
	}

	public NetPeer Connect(IPEndPoint target, NetDataWriter connectionData)
	{
		if (!IsRunning)
		{
			throw new InvalidOperationException("Client is not running");
		}
		lock (_requestsDict)
		{
			if (_requestsDict.ContainsKey(target))
			{
				return null;
			}
		}
		byte connectNum = 0;
		_peersLock.EnterUpgradeableReadLock();
		if (_peersDict.TryGetValue(target, out var value))
		{
			ConnectionState connectionState = value.ConnectionState;
			if (connectionState == ConnectionState.Outgoing || connectionState == ConnectionState.Connected)
			{
				_peersLock.ExitUpgradeableReadLock();
				return value;
			}
			connectNum = (byte)((value.ConnectionNum + 1) % 4);
			RemovePeer(value);
		}
		value = new NetPeer(this, target, GetNextPeerId(), connectNum, connectionData);
		AddPeer(value);
		_peersLock.ExitUpgradeableReadLock();
		return value;
	}

	public void Stop()
	{
		Stop(sendDisconnectMessages: true);
	}

	public void Stop(bool sendDisconnectMessages)
	{
		if (IsRunning)
		{
			_pausedSocketFix.Deinitialize();
			_pausedSocketFix = null;
			for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				netPeer.Shutdown(null, 0, 0, !sendDisconnectMessages);
			}
			CloseSocket();
			_updateTriggerEvent.Set();
			if (!_manualMode)
			{
				_logicThread.Join();
				_logicThread = null;
			}
			_peersLock.EnterWriteLock();
			_headPeer = null;
			_peersDict.Clear();
			_peersArray = new NetPeer[32];
			_peersLock.ExitWriteLock();
			_peerIds = new ConcurrentQueue<int>();
			_lastPeerId = 0;
			_connectedPeersCount = 0;
			_pendingEventHead = null;
			_pendingEventTail = null;
		}
	}

	public int GetPeersCount(ConnectionState peerState)
	{
		int num = 0;
		_peersLock.EnterReadLock();
		for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if ((netPeer.ConnectionState & peerState) != 0)
			{
				num++;
			}
		}
		_peersLock.ExitReadLock();
		return num;
	}

	public void GetPeersNonAlloc(List<NetPeer> peers, ConnectionState peerState)
	{
		peers.Clear();
		_peersLock.EnterReadLock();
		for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if ((netPeer.ConnectionState & peerState) != 0)
			{
				peers.Add(netPeer);
			}
		}
		_peersLock.ExitReadLock();
	}

	public void DisconnectAll()
	{
		DisconnectAll(null, 0, 0);
	}

	public void DisconnectAll(byte[] data, int start, int count)
	{
		_peersLock.EnterReadLock();
		for (NetPeer netPeer = _headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			DisconnectPeer(netPeer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, force: false, data, start, count, null);
		}
		_peersLock.ExitReadLock();
	}

	public void DisconnectPeerForce(NetPeer peer)
	{
		DisconnectPeerForce(peer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, null);
	}

	public void DisconnectPeer(NetPeer peer)
	{
		DisconnectPeer(peer, null, 0, 0);
	}

	public void DisconnectPeer(NetPeer peer, byte[] data)
	{
		DisconnectPeer(peer, data, 0, data.Length);
	}

	public void DisconnectPeer(NetPeer peer, NetDataWriter writer)
	{
		DisconnectPeer(peer, writer.Data, 0, writer.Length);
	}

	public void DisconnectPeer(NetPeer peer, byte[] data, int start, int count)
	{
		DisconnectPeer(peer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, force: false, data, start, count, null);
	}

	public void CreateNtpRequest(IPEndPoint endPoint)
	{
		_ntpRequests.Add(endPoint, new NtpRequest(endPoint));
	}

	public void CreateNtpRequest(string ntpServerAddress, int port)
	{
		IPEndPoint iPEndPoint = NetUtils.MakeEndPoint(ntpServerAddress, port);
		_ntpRequests.Add(iPEndPoint, new NtpRequest(iPEndPoint));
	}

	public void CreateNtpRequest(string ntpServerAddress)
	{
		IPEndPoint iPEndPoint = NetUtils.MakeEndPoint(ntpServerAddress, 123);
		_ntpRequests.Add(iPEndPoint, new NtpRequest(iPEndPoint));
	}

	public NetPeerEnumerator GetEnumerator()
	{
		return new NetPeerEnumerator(_headPeer);
	}

	IEnumerator<NetPeer> IEnumerable<NetPeer>.GetEnumerator()
	{
		return new NetPeerEnumerator(_headPeer);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new NetPeerEnumerator(_headPeer);
	}

	private NetPacket PoolGetWithData(PacketProperty property, byte[] data, int start, int length)
	{
		int headerSize = NetPacket.GetHeaderSize(property);
		NetPacket netPacket = PoolGetPacket(length + headerSize);
		netPacket.Property = property;
		Buffer.BlockCopy(data, start, netPacket.RawData, headerSize, length);
		return netPacket;
	}

	private NetPacket PoolGetWithProperty(PacketProperty property, int size)
	{
		NetPacket netPacket = PoolGetPacket(size + NetPacket.GetHeaderSize(property));
		netPacket.Property = property;
		return netPacket;
	}

	private NetPacket PoolGetWithProperty(PacketProperty property)
	{
		NetPacket netPacket = PoolGetPacket(NetPacket.GetHeaderSize(property));
		netPacket.Property = property;
		return netPacket;
	}

	internal NetPacket PoolGetPacket(int size)
	{
		if (size > NetConstants.MaxPacketSize)
		{
			return new NetPacket(size);
		}
		NetPacket poolHead;
		lock (_poolLock)
		{
			poolHead = _poolHead;
			if (poolHead == null)
			{
				return new NetPacket(size);
			}
			_poolHead = _poolHead.Next;
			_poolCount--;
		}
		poolHead.Size = size;
		if (poolHead.RawData.Length < size)
		{
			poolHead.RawData = new byte[size];
		}
		return poolHead;
	}

	internal void PoolRecycle(NetPacket packet)
	{
		if (packet.RawData.Length > NetConstants.MaxPacketSize || _poolCount >= PacketPoolSize)
		{
			return;
		}
		packet.RawData[0] = 0;
		lock (_poolLock)
		{
			packet.Next = _poolHead;
			_poolHead = packet;
			_poolCount++;
		}
	}

	static NetManager()
	{
		MulticastAddressV6 = IPAddress.Parse("ff02::1");
		IPv6Support = Socket.OSSupportsIPv6;
	}

	private void RegisterEndPoint(IPEndPoint ep)
	{
		if (UseNativeSockets && ep is NativeEndPoint nativeEndPoint)
		{
			_nativeAddrMap.Add(new NativeAddr(nativeEndPoint.NativeAddress, nativeEndPoint.NativeAddress.Length), nativeEndPoint);
		}
	}

	private void UnregisterEndPoint(IPEndPoint ep)
	{
		if (UseNativeSockets && ep is NativeEndPoint nativeEndPoint)
		{
			NativeAddr key = new NativeAddr(nativeEndPoint.NativeAddress, nativeEndPoint.NativeAddress.Length);
			_nativeAddrMap.Remove(key);
		}
	}

	private bool ProcessError(SocketException ex)
	{
		switch (ex.SocketErrorCode)
		{
		case SocketError.NotConnected:
			NotConnected = true;
			return true;
		case SocketError.OperationAborted:
		case SocketError.Interrupted:
		case SocketError.NotSocket:
			return true;
		default:
			NetDebug.WriteError($"[R]Error code: {(int)ex.SocketErrorCode} - {ex}");
			CreateEvent(NetEvent.EType.Error, null, null, ex.SocketErrorCode, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
			break;
		case SocketError.MessageSize:
		case SocketError.NetworkReset:
		case SocketError.ConnectionReset:
		case SocketError.TimedOut:
			break;
		}
		return false;
	}

	private void ManualReceive(Socket socket, EndPoint bufferEndPoint)
	{
		try
		{
			int num = 0;
			while (socket.Available > 0)
			{
				ReceiveFrom(socket, ref bufferEndPoint);
				num++;
				if (num == MaxPacketsReceivePerUpdate)
				{
					break;
				}
			}
		}
		catch (SocketException ex)
		{
			ProcessError(ex);
		}
		catch (ObjectDisposedException)
		{
		}
		catch (Exception ex3)
		{
			NetDebug.WriteError("[NM] SocketReceiveThread error: " + ex3);
		}
	}

	private bool NativeReceiveFrom(ref NetPacket packet, IntPtr s, byte[] addrBuffer, int addrSize)
	{
		packet.Size = NativeSocket.RecvFrom(s, packet.RawData, NetConstants.MaxPacketSize, addrBuffer, ref addrSize);
		if (packet.Size == 0)
		{
			return false;
		}
		if (packet.Size == -1)
		{
			SocketError socketError = NativeSocket.GetSocketError();
			if (socketError != SocketError.WouldBlock && socketError != SocketError.TimedOut)
			{
				return !ProcessError(new SocketException((int)socketError));
			}
			return true;
		}
		NativeAddr key = new NativeAddr(addrBuffer, addrSize);
		if (!_nativeAddrMap.TryGetValue(key, out var value))
		{
			value = new NativeEndPoint(addrBuffer);
		}
		OnMessageReceived(packet, value);
		packet = PoolGetPacket(NetConstants.MaxPacketSize);
		return true;
	}

	private void NativeReceiveLogic()
	{
		IntPtr handle = _udpSocketv4.Handle;
		IntPtr s = _udpSocketv6?.Handle ?? IntPtr.Zero;
		byte[] array = new byte[16];
		byte[] array2 = new byte[28];
		int addrSize = array.Length;
		int addrSize2 = array2.Length;
		List<Socket> list = new List<Socket>(2);
		Socket udpSocketv = _udpSocketv4;
		Socket udpSocketv2 = _udpSocketv6;
		NetPacket packet = PoolGetPacket(NetConstants.MaxPacketSize);
		while (IsRunning)
		{
			try
			{
				if (udpSocketv2 == null)
				{
					NativeReceiveFrom(ref packet, handle, array, addrSize);
				}
				bool flag = false;
				if (udpSocketv.Available != 0 || list.Contains(udpSocketv))
				{
					NativeReceiveFrom(ref packet, handle, array, addrSize);
					flag = true;
				}
				if (udpSocketv2.Available != 0 || list.Contains(udpSocketv2))
				{
					NativeReceiveFrom(ref packet, s, array2, addrSize2);
					flag = true;
				}
				list.Clear();
				if (!flag)
				{
					list.Add(udpSocketv);
					list.Add(udpSocketv2);
					Socket.Select(list, null, null, 500000);
				}
			}
			catch (SocketException ex)
			{
				if (ProcessError(ex))
				{
					break;
				}
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (ThreadAbortException)
			{
				break;
			}
			catch (Exception ex4)
			{
				NetDebug.WriteError("[NM] SocketReceiveThread error: " + ex4);
			}
		}
	}

	private void ReceiveFrom(Socket s, ref EndPoint bufferEndPoint)
	{
		NetPacket netPacket = PoolGetPacket(NetConstants.MaxPacketSize);
		netPacket.Size = s.ReceiveFrom(netPacket.RawData, 0, NetConstants.MaxPacketSize, SocketFlags.None, ref bufferEndPoint);
		OnMessageReceived(netPacket, (IPEndPoint)bufferEndPoint);
	}

	private void ReceiveLogic()
	{
		EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
		EndPoint bufferEndPoint2 = new IPEndPoint(IPAddress.IPv6Any, 0);
		List<Socket> list = new List<Socket>(2);
		Socket udpSocketv = _udpSocketv4;
		Socket udpSocketv2 = _udpSocketv6;
		while (IsRunning)
		{
			try
			{
				if (udpSocketv2 == null)
				{
					if (udpSocketv.Available != 0 || udpSocketv.Poll(500000, SelectMode.SelectRead))
					{
						ReceiveFrom(udpSocketv, ref bufferEndPoint);
					}
					continue;
				}
				bool flag = false;
				if (udpSocketv.Available != 0 || list.Contains(udpSocketv))
				{
					ReceiveFrom(udpSocketv, ref bufferEndPoint);
					flag = true;
				}
				if (udpSocketv2.Available != 0 || list.Contains(udpSocketv2))
				{
					ReceiveFrom(udpSocketv2, ref bufferEndPoint2);
					flag = true;
				}
				list.Clear();
				if (!flag)
				{
					list.Add(udpSocketv);
					list.Add(udpSocketv2);
					Socket.Select(list, null, null, 500000);
				}
			}
			catch (SocketException ex)
			{
				if (ProcessError(ex))
				{
					break;
				}
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (ThreadAbortException)
			{
				break;
			}
			catch (Exception ex4)
			{
				NetDebug.WriteError("[NM] SocketReceiveThread error: " + ex4);
			}
		}
	}

	public bool Start(IPAddress addressIPv4, IPAddress addressIPv6, int port, bool manualMode)
	{
		if (IsRunning && !NotConnected)
		{
			return false;
		}
		NotConnected = false;
		_manualMode = manualMode;
		UseNativeSockets = UseNativeSockets && NativeSocket.IsSupported;
		_udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		if (!BindSocket(_udpSocketv4, new IPEndPoint(addressIPv4, port)))
		{
			return false;
		}
		LocalPort = ((IPEndPoint)_udpSocketv4.LocalEndPoint).Port;
		if (_pausedSocketFix == null)
		{
			_pausedSocketFix = new PausedSocketFix(this, addressIPv4, addressIPv6, port, manualMode);
		}
		IsRunning = true;
		if (_manualMode)
		{
			_bufferEndPointv4 = new IPEndPoint(IPAddress.Any, 0);
		}
		if (IPv6Support && IPv6Enabled)
		{
			_udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			if (BindSocket(_udpSocketv6, new IPEndPoint(addressIPv6, LocalPort)))
			{
				if (_manualMode)
				{
					_bufferEndPointv6 = new IPEndPoint(IPAddress.IPv6Any, 0);
				}
			}
			else
			{
				_udpSocketv6 = null;
			}
		}
		if (!manualMode)
		{
			ThreadStart start = ReceiveLogic;
			if (UseNativeSockets)
			{
				start = NativeReceiveLogic;
			}
			_receiveThread = new Thread(start)
			{
				Name = $"ReceiveThread({LocalPort})",
				IsBackground = true
			};
			_receiveThread.Start();
			if (_logicThread == null)
			{
				_logicThread = new Thread(UpdateLogic)
				{
					Name = "LogicThread",
					IsBackground = true
				};
				_logicThread.Start();
			}
		}
		return true;
	}

	private bool BindSocket(Socket socket, IPEndPoint ep)
	{
		socket.ReceiveTimeout = 500;
		socket.SendTimeout = 500;
		socket.ReceiveBufferSize = 1048576;
		socket.SendBufferSize = 1048576;
		socket.Blocking = true;
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			try
			{
				socket.IOControl(-1744830452, new byte[1], null);
			}
			catch
			{
			}
		}
		try
		{
			socket.ExclusiveAddressUse = !ReuseAddress;
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, ReuseAddress);
		}
		catch
		{
		}
		if (ep.AddressFamily == AddressFamily.InterNetwork)
		{
			Ttl = 255;
			try
			{
				socket.EnableBroadcast = true;
			}
			catch (SocketException ex)
			{
				NetDebug.WriteError($"[B]Broadcast error: {ex.SocketErrorCode}");
			}
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				try
				{
					socket.DontFragment = true;
				}
				catch (SocketException ex2)
				{
					NetDebug.WriteError($"[B]DontFragment error: {ex2.SocketErrorCode}");
				}
			}
		}
		try
		{
			socket.Bind(ep);
			_ = ep.AddressFamily;
			_ = 23;
		}
		catch (SocketException ex3)
		{
			switch (ex3.SocketErrorCode)
			{
			case SocketError.AddressAlreadyInUse:
				if (socket.AddressFamily == AddressFamily.InterNetworkV6)
				{
					try
					{
						socket.DualMode = false;
						socket.Bind(ep);
					}
					catch (SocketException ex4)
					{
						NetDebug.WriteError($"[B]Bind exception: {ex4}, errorCode: {ex4.SocketErrorCode}");
						return false;
					}
					return true;
				}
				break;
			case SocketError.AddressFamilyNotSupported:
				return true;
			}
			NetDebug.WriteError($"[B]Bind exception: {ex3}, errorCode: {ex3.SocketErrorCode}");
			return false;
		}
		return true;
	}

	internal int SendRawAndRecycle(NetPacket packet, IPEndPoint remoteEndPoint)
	{
		int result = SendRaw(packet.RawData, 0, packet.Size, remoteEndPoint);
		PoolRecycle(packet);
		return result;
	}

	internal int SendRaw(NetPacket packet, IPEndPoint remoteEndPoint)
	{
		return SendRaw(packet.RawData, 0, packet.Size, remoteEndPoint);
	}

	internal int SendRaw(byte[] message, int start, int length, IPEndPoint remoteEndPoint)
	{
		if (!IsRunning)
		{
			return 0;
		}
		NetPacket netPacket = null;
		if (_extraPacketLayer != null)
		{
			netPacket = PoolGetPacket(length + _extraPacketLayer.ExtraPacketSizeForLayer);
			Buffer.BlockCopy(message, start, netPacket.RawData, 0, length);
			start = 0;
			_extraPacketLayer.ProcessOutBoundPacket(ref remoteEndPoint, ref netPacket.RawData, ref start, ref length);
			message = netPacket.RawData;
		}
		Socket socket = _udpSocketv4;
		if (remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 && IPv6Support)
		{
			socket = _udpSocketv6;
			if (socket == null)
			{
				return 0;
			}
		}
		int num2;
		try
		{
			if (UseNativeSockets)
			{
				byte[] array;
				if (remoteEndPoint is NativeEndPoint nativeEndPoint)
				{
					array = nativeEndPoint.NativeAddress;
				}
				else
				{
					if (_endPointBuffer == null)
					{
						_endPointBuffer = new byte[28];
					}
					array = _endPointBuffer;
					bool num = remoteEndPoint.AddressFamily == AddressFamily.InterNetwork;
					short nativeAddressFamily = NativeSocket.GetNativeAddressFamily(remoteEndPoint);
					array[0] = (byte)nativeAddressFamily;
					array[1] = (byte)(nativeAddressFamily >> 8);
					array[2] = (byte)(remoteEndPoint.Port >> 8);
					array[3] = (byte)remoteEndPoint.Port;
					if (num)
					{
						long address = remoteEndPoint.Address.Address;
						array[4] = (byte)address;
						array[5] = (byte)(address >> 8);
						array[6] = (byte)(address >> 16);
						array[7] = (byte)(address >> 24);
					}
					else
					{
						Buffer.BlockCopy(remoteEndPoint.Address.GetAddressBytes(), 0, array, 8, 16);
					}
				}
				if (start > 0)
				{
					if (_sendToBuffer == null)
					{
						_sendToBuffer = new byte[NetConstants.MaxPacketSize];
					}
					Buffer.BlockCopy(message, start, _sendToBuffer, 0, length);
					message = _sendToBuffer;
				}
				num2 = NativeSocket.SendTo(socket.Handle, message, length, array, array.Length);
				if (num2 == -1)
				{
					throw NativeSocket.GetSocketException();
				}
			}
			else
			{
				num2 = socket.SendTo(message, start, length, SocketFlags.None, remoteEndPoint);
			}
		}
		catch (SocketException ex)
		{
			switch (ex.SocketErrorCode)
			{
			case SocketError.Interrupted:
			case SocketError.NoBufferSpaceAvailable:
				return 0;
			case SocketError.MessageSize:
				return 0;
			case SocketError.NetworkUnreachable:
			case SocketError.HostUnreachable:
			{
				if (DisconnectOnUnreachable && TryGetPeer(remoteEndPoint, out var peer))
				{
					DisconnectPeerForce(peer, (ex.SocketErrorCode == SocketError.HostUnreachable) ? DisconnectReason.HostUnreachable : DisconnectReason.NetworkUnreachable, ex.SocketErrorCode, null);
				}
				CreateEvent(NetEvent.EType.Error, null, remoteEndPoint, ex.SocketErrorCode, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
				return -1;
			}
			default:
				NetDebug.WriteError($"[S] {ex}");
				return -1;
			}
		}
		catch (Exception arg)
		{
			NetDebug.WriteError($"[S] {arg}");
			return 0;
		}
		finally
		{
			if (netPacket != null)
			{
				PoolRecycle(netPacket);
			}
		}
		if (num2 <= 0)
		{
			return 0;
		}
		if (EnableStatistics)
		{
			Statistics.IncrementPacketsSent();
			Statistics.AddBytesSent(length);
		}
		return num2;
	}

	public bool SendBroadcast(NetDataWriter writer, int port)
	{
		return SendBroadcast(writer.Data, 0, writer.Length, port);
	}

	public bool SendBroadcast(byte[] data, int port)
	{
		return SendBroadcast(data, 0, data.Length, port);
	}

	public bool SendBroadcast(byte[] data, int start, int length, int port)
	{
		if (!IsRunning)
		{
			return false;
		}
		NetPacket netPacket;
		if (_extraPacketLayer != null)
		{
			int headerSize = NetPacket.GetHeaderSize(PacketProperty.Broadcast);
			netPacket = PoolGetPacket(headerSize + length + _extraPacketLayer.ExtraPacketSizeForLayer);
			netPacket.Property = PacketProperty.Broadcast;
			Buffer.BlockCopy(data, start, netPacket.RawData, headerSize, length);
			int offset = 0;
			int length2 = length + headerSize;
			IPEndPoint endPoint = null;
			_extraPacketLayer.ProcessOutBoundPacket(ref endPoint, ref netPacket.RawData, ref offset, ref length2);
		}
		else
		{
			netPacket = PoolGetWithData(PacketProperty.Broadcast, data, start, length);
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = _udpSocketv4.SendTo(netPacket.RawData, 0, netPacket.Size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port)) > 0;
			if (_udpSocketv6 != null)
			{
				flag2 = _udpSocketv6.SendTo(netPacket.RawData, 0, netPacket.Size, SocketFlags.None, new IPEndPoint(MulticastAddressV6, port)) > 0;
			}
		}
		catch (Exception arg)
		{
			NetDebug.WriteError($"[S][MCAST] {arg}");
			return flag;
		}
		finally
		{
			PoolRecycle(netPacket);
		}
		return flag || flag2;
	}

	private void CloseSocket()
	{
		IsRunning = false;
		_udpSocketv4?.Close();
		_udpSocketv6?.Close();
		_udpSocketv4 = null;
		_udpSocketv6 = null;
		if (_receiveThread != null && _receiveThread != Thread.CurrentThread)
		{
			_receiveThread.Join();
		}
		_receiveThread = null;
	}
}

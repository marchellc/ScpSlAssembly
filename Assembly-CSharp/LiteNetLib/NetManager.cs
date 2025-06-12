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

		public NetPeer Current => this._p;

		object IEnumerator.Current => this._p;

		public NetPeerEnumerator(NetPeer p)
		{
			this._initialPeer = p;
			this._p = null;
		}

		public void Dispose()
		{
		}

		public bool MoveNext()
		{
			this._p = ((this._p == null) ? this._initialPeer : this._p.NextPeer);
			return this._p != null;
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

	public NetPeer FirstPeer => this._headPeer;

	public byte ChannelsCount
	{
		get
		{
			return this._channelsCount;
		}
		set
		{
			if (value < 1 || value > 64)
			{
				throw new ArgumentException("Channels count must be between 1 and 64");
			}
			this._channelsCount = value;
		}
	}

	public List<NetPeer> ConnectedPeerList
	{
		get
		{
			this.GetPeersNonAlloc(this._connectedPeerListCache, ConnectionState.Connected);
			return this._connectedPeerListCache;
		}
	}

	public int ConnectedPeersCount => Interlocked.CompareExchange(ref this._connectedPeersCount, 0, 0);

	public int ExtraPacketSizeForLayer => this._extraPacketLayer?.ExtraPacketSizeForLayer ?? 0;

	public int PoolCount => this._poolCount;

	public short Ttl
	{
		get
		{
			return this._udpSocketv4.Ttl;
		}
		internal set
		{
			this._udpSocketv4.Ttl = value;
		}
	}

	public NetPeer GetPeerById(int id)
	{
		if (id >= 0 && id < this._peersArray.Length)
		{
			return this._peersArray[id];
		}
		return null;
	}

	public bool TryGetPeerById(int id, out NetPeer peer)
	{
		peer = this.GetPeerById(id);
		return peer != null;
	}

	private bool TryGetPeer(IPEndPoint endPoint, out NetPeer peer)
	{
		this._peersLock.EnterReadLock();
		bool result = this._peersDict.TryGetValue(endPoint, out peer);
		this._peersLock.ExitReadLock();
		return result;
	}

	private void AddPeer(NetPeer peer)
	{
		this._peersLock.EnterWriteLock();
		if (this._headPeer != null)
		{
			peer.NextPeer = this._headPeer;
			this._headPeer.PrevPeer = peer;
		}
		this._headPeer = peer;
		this._peersDict.Add(peer.EndPoint, peer);
		if (peer.Id >= this._peersArray.Length)
		{
			int num = this._peersArray.Length * 2;
			while (peer.Id >= num)
			{
				num *= 2;
			}
			Array.Resize(ref this._peersArray, num);
		}
		this._peersArray[peer.Id] = peer;
		this.RegisterEndPoint(peer.EndPoint);
		this._peersLock.ExitWriteLock();
	}

	private void RemovePeer(NetPeer peer)
	{
		this._peersLock.EnterWriteLock();
		this.RemovePeerInternal(peer);
		this._peersLock.ExitWriteLock();
	}

	private void RemovePeerInternal(NetPeer peer)
	{
		if (this._peersDict.Remove(peer.EndPoint))
		{
			if (peer == this._headPeer)
			{
				this._headPeer = peer.NextPeer;
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
			this._peersArray[peer.Id] = null;
			this._peerIds.Enqueue(peer.Id);
			this.UnregisterEndPoint(peer.EndPoint);
		}
	}

	public NetManager(INetEventListener listener, PacketLayerBase extraPacketLayer = null)
	{
		this._netEventListener = listener;
		this._deliveryEventListener = listener as IDeliveryEventListener;
		this._ntpEventListener = listener as INtpEventListener;
		this._peerAddressChangedListener = listener as IPeerAddressChangedListener;
		this.NatPunchModule = new NatPunchModule(this);
		this._extraPacketLayer = extraPacketLayer;
	}

	internal void ConnectionLatencyUpdated(NetPeer fromPeer, int latency)
	{
		this.CreateEvent(NetEvent.EType.ConnectionLatencyUpdated, fromPeer, null, SocketError.Success, latency, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
	}

	internal void MessageDelivered(NetPeer fromPeer, object userData)
	{
		if (this._deliveryEventListener != null)
		{
			this.CreateEvent(NetEvent.EType.MessageDelivered, fromPeer, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, null, userData);
		}
	}

	internal void DisconnectPeerForce(NetPeer peer, DisconnectReason reason, SocketError socketErrorCode, NetPacket eventData)
	{
		this.DisconnectPeer(peer, reason, socketErrorCode, force: true, null, 0, 0, eventData);
	}

	private void DisconnectPeer(NetPeer peer, DisconnectReason reason, SocketError socketErrorCode, bool force, byte[] data, int start, int count, NetPacket eventData)
	{
		switch (peer.Shutdown(data, start, count, force))
		{
		case ShutdownResult.None:
			return;
		case ShutdownResult.WasConnected:
			Interlocked.Decrement(ref this._connectedPeersCount);
			break;
		}
		this.CreateEvent(NetEvent.EType.Disconnect, peer, null, socketErrorCode, 0, reason, null, DeliveryMethod.Unreliable, 0, eventData);
	}

	private void CreateEvent(NetEvent.EType type, NetPeer peer = null, IPEndPoint remoteEndPoint = null, SocketError errorCode = SocketError.Success, int latency = 0, DisconnectReason disconnectReason = DisconnectReason.ConnectionFailed, ConnectionRequest connectionRequest = null, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, byte channelNumber = 0, NetPacket readerSource = null, object userData = null)
	{
		bool flag = this.UnsyncedEvents;
		switch (type)
		{
		case NetEvent.EType.Connect:
			Interlocked.Increment(ref this._connectedPeersCount);
			break;
		case NetEvent.EType.MessageDelivered:
			flag = this.UnsyncedDeliveryEvent;
			break;
		}
		NetEvent netEvent;
		lock (this._eventLock)
		{
			netEvent = this._netEventPoolHead;
			if (netEvent == null)
			{
				netEvent = new NetEvent(this);
			}
			else
			{
				this._netEventPoolHead = netEvent.Next;
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
		if (flag || this._manualMode)
		{
			this.ProcessEvent(netEvent);
			return;
		}
		lock (this._eventLock)
		{
			if (this._pendingEventTail == null)
			{
				this._pendingEventHead = netEvent;
			}
			else
			{
				this._pendingEventTail.Next = netEvent;
			}
			this._pendingEventTail = netEvent;
		}
	}

	private void ProcessEvent(NetEvent evt)
	{
		bool isNull = evt.DataReader.IsNull;
		switch (evt.Type)
		{
		case NetEvent.EType.Connect:
			this._netEventListener.OnPeerConnected(evt.Peer);
			break;
		case NetEvent.EType.Disconnect:
		{
			DisconnectInfo disconnectInfo = new DisconnectInfo
			{
				Reason = evt.DisconnectReason,
				AdditionalData = evt.DataReader,
				SocketErrorCode = evt.ErrorCode
			};
			this._netEventListener.OnPeerDisconnected(evt.Peer, disconnectInfo);
			break;
		}
		case NetEvent.EType.Receive:
			this._netEventListener.OnNetworkReceive(evt.Peer, evt.DataReader, evt.ChannelNumber, evt.DeliveryMethod);
			break;
		case NetEvent.EType.ReceiveUnconnected:
			this._netEventListener.OnNetworkReceiveUnconnected(evt.RemoteEndPoint, evt.DataReader, UnconnectedMessageType.BasicMessage);
			break;
		case NetEvent.EType.Broadcast:
			this._netEventListener.OnNetworkReceiveUnconnected(evt.RemoteEndPoint, evt.DataReader, UnconnectedMessageType.Broadcast);
			break;
		case NetEvent.EType.Error:
			this._netEventListener.OnNetworkError(evt.RemoteEndPoint, evt.ErrorCode);
			break;
		case NetEvent.EType.ConnectionLatencyUpdated:
			this._netEventListener.OnNetworkLatencyUpdate(evt.Peer, evt.Latency);
			break;
		case NetEvent.EType.ConnectionRequest:
			this._netEventListener.OnConnectionRequest(evt.ConnectionRequest);
			break;
		case NetEvent.EType.MessageDelivered:
			this._deliveryEventListener.OnMessageDelivered(evt.Peer, evt.UserData);
			break;
		case NetEvent.EType.PeerAddressChanged:
		{
			this._peersLock.EnterUpgradeableReadLock();
			IPEndPoint iPEndPoint = null;
			if (this._peersDict.ContainsKey(evt.Peer.EndPoint))
			{
				this._peersLock.EnterWriteLock();
				this._peersDict.Remove(evt.Peer.EndPoint);
				iPEndPoint = evt.Peer.EndPoint;
				evt.Peer.FinishEndPointChange(evt.RemoteEndPoint);
				this._peersDict.Add(evt.Peer.EndPoint, evt.Peer);
				this._peersLock.ExitWriteLock();
			}
			this._peersLock.ExitUpgradeableReadLock();
			if (iPEndPoint != null)
			{
				this._peerAddressChangedListener.OnPeerAddressChanged(evt.Peer, iPEndPoint);
			}
			break;
		}
		}
		if (isNull)
		{
			this.RecycleEvent(evt);
		}
		else if (this.AutoRecycle)
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
		lock (this._eventLock)
		{
			evt.Next = this._netEventPoolHead;
			this._netEventPoolHead = evt;
		}
	}

	private void UpdateLogic()
	{
		List<NetPeer> list = new List<NetPeer>();
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		while (this.IsRunning)
		{
			try
			{
				int num = (int)stopwatch.ElapsedMilliseconds;
				num = ((num <= 0) ? 1 : num);
				stopwatch.Restart();
				for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
				{
					if (netPeer.ConnectionState == ConnectionState.Disconnected && netPeer.TimeSinceLastPacket > this.DisconnectTimeout)
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
					this._peersLock.EnterWriteLock();
					for (int i = 0; i < list.Count; i++)
					{
						this.RemovePeerInternal(list[i]);
					}
					this._peersLock.ExitWriteLock();
					list.Clear();
				}
				this.ProcessNtpRequests(num);
				int num2 = this.UpdateTime - (int)stopwatch.ElapsedMilliseconds;
				if (num2 > 0)
				{
					this._updateTriggerEvent.WaitOne(num2);
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
		foreach (KeyValuePair<IPEndPoint, NtpRequest> ntpRequest in this._ntpRequests)
		{
			ntpRequest.Value.Send(this._udpSocketv4, elapsedMilliseconds);
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
			this._ntpRequests.Remove(item);
		}
	}

	public void ManualUpdate(int elapsedMilliseconds)
	{
		if (!this._manualMode)
		{
			return;
		}
		for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if (netPeer.ConnectionState == ConnectionState.Disconnected && netPeer.TimeSinceLastPacket > this.DisconnectTimeout)
			{
				this.RemovePeerInternal(netPeer);
			}
			else
			{
				netPeer.Update(elapsedMilliseconds);
			}
		}
		this.ProcessNtpRequests(elapsedMilliseconds);
	}

	internal NetPeer OnConnectionSolved(ConnectionRequest request, byte[] rejectData, int start, int length)
	{
		NetPeer value = null;
		if (request.Result == ConnectionRequestResult.RejectForce)
		{
			if (rejectData != null && length > 0)
			{
				NetPacket netPacket = this.PoolGetWithProperty(PacketProperty.Disconnect, length);
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
				this.SendRawAndRecycle(netPacket, request.RemoteEndPoint);
			}
		}
		else
		{
			this._peersLock.EnterUpgradeableReadLock();
			if (this._peersDict.TryGetValue(request.RemoteEndPoint, out value))
			{
				this._peersLock.ExitUpgradeableReadLock();
			}
			else if (request.Result == ConnectionRequestResult.Reject)
			{
				value = new NetPeer(this, request.RemoteEndPoint, this.GetNextPeerId());
				value.Reject(request.InternalPacket, rejectData, start, length);
				this.AddPeer(value);
				this._peersLock.ExitUpgradeableReadLock();
			}
			else
			{
				value = new NetPeer(this, request, this.GetNextPeerId());
				this.AddPeer(value);
				this._peersLock.ExitUpgradeableReadLock();
				this.CreateEvent(NetEvent.EType.Connect, value, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
			}
		}
		lock (this._requestsDict)
		{
			this._requestsDict.Remove(request.RemoteEndPoint);
			return value;
		}
	}

	private int GetNextPeerId()
	{
		if (!this._peerIds.TryDequeue(out var result))
		{
			return this._lastPeerId++;
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
				this.DisconnectPeerForce(netPeer, DisconnectReason.Reconnect, SocketError.Success, null);
				this.RemovePeer(netPeer);
				break;
			case ConnectRequestResult.NewConnection:
				this.RemovePeer(netPeer);
				break;
			case ConnectRequestResult.P2PLose:
				this.DisconnectPeerForce(netPeer, DisconnectReason.PeerToPeerConnection, SocketError.Success, null);
				this.RemovePeer(netPeer);
				break;
			}
			if (connectRequestResult != ConnectRequestResult.P2PLose)
			{
				connRequest.ConnectionNumber = (byte)((netPeer.ConnectionNum + 1) % 4);
			}
		}
		ConnectionRequest value;
		lock (this._requestsDict)
		{
			if (this._requestsDict.TryGetValue(remoteEndPoint, out value))
			{
				value.UpdateRequest(connRequest);
				return;
			}
			value = new ConnectionRequest(remoteEndPoint, connRequest, this);
			this._requestsDict.Add(remoteEndPoint, value);
		}
		this.CreateEvent(NetEvent.EType.ConnectionRequest, null, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, value, DeliveryMethod.Unreliable, 0);
	}

	private void OnMessageReceived(NetPacket packet, IPEndPoint remoteEndPoint)
	{
		int size = packet.Size;
		if (this.EnableStatistics)
		{
			this.Statistics.IncrementPacketsReceived();
			this.Statistics.AddBytesReceived(size);
		}
		if (this._ntpRequests.Count > 0 && this._ntpRequests.TryGetValue(remoteEndPoint, out var _))
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
					this._ntpRequests.Remove(remoteEndPoint);
					this._ntpEventListener?.OnNtpResponse(ntpPacket);
				}
			}
			return;
		}
		if (this._extraPacketLayer != null)
		{
			int offset = 0;
			this._extraPacketLayer.ProcessInboundPacket(ref remoteEndPoint, ref packet.RawData, ref offset, ref packet.Size);
			if (packet.Size == 0)
			{
				return;
			}
		}
		if (!packet.Verify())
		{
			if (packet.RawData.Length >= 5 && packet.RawData[4] == 84)
			{
				this._udpSocketv4.SendTo(SteamServerInfo.Serialize(), SocketFlags.None, remoteEndPoint);
				this.PoolRecycle(packet);
			}
			else
			{
				NetDebug.WriteError("[NM] DataReceived: bad!");
				this.PoolRecycle(packet);
			}
			return;
		}
		switch (packet.Property)
		{
		case PacketProperty.ConnectRequest:
			if (NetConnectRequestPacket.GetProtocolId(packet) != 13)
			{
				this.SendRawAndRecycle(this.PoolGetWithProperty(PacketProperty.InvalidProtocol), remoteEndPoint);
				return;
			}
			break;
		case PacketProperty.Broadcast:
			if (this.BroadcastReceiveEnabled)
			{
				this.CreateEvent(NetEvent.EType.Broadcast, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
			}
			return;
		case PacketProperty.UnconnectedMessage:
			if (this.UnconnectedMessagesEnabled)
			{
				this.CreateEvent(NetEvent.EType.ReceiveUnconnected, null, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0, packet);
			}
			return;
		case PacketProperty.NatMessage:
			if (this.NatPunchEnabled)
			{
				this.NatPunchModule.ProcessMessage(remoteEndPoint, packet);
			}
			return;
		}
		this._peersLock.EnterReadLock();
		NetPeer value2;
		bool flag = this._peersDict.TryGetValue(remoteEndPoint, out value2);
		this._peersLock.ExitReadLock();
		if (flag && this.EnableStatistics)
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
				this.ProcessConnectRequest(remoteEndPoint, value2, netConnectRequestPacket);
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
						this.SendRaw(NetConnectAcceptPacket.MakeNetworkChanged(value2), remoteEndPoint);
					}
					else if (packet.Size == 2 && packet.RawData[1] == 1)
					{
						this.DisconnectPeerForce(value2, DisconnectReason.PeerNotFound, SocketError.Success, null);
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
				if (this.AllowPeerAddressChange)
				{
					NetConnectAcceptPacket netConnectAcceptPacket = NetConnectAcceptPacket.FromData(packet);
					if (netConnectAcceptPacket != null && netConnectAcceptPacket.PeerNetworkChanged && netConnectAcceptPacket.PeerId < this._peersArray.Length)
					{
						this._peersLock.EnterUpgradeableReadLock();
						NetPeer netPeer = this._peersArray[netConnectAcceptPacket.PeerId];
						if (netPeer != null && netPeer.ConnectTime == netConnectAcceptPacket.ConnectionTime && netPeer.ConnectionNum == netConnectAcceptPacket.ConnectionNumber)
						{
							if (netPeer.ConnectionState == ConnectionState.Connected)
							{
								netPeer.InitiateEndPointChange();
								if (this._peerAddressChangedListener != null)
								{
									this.CreateEvent(NetEvent.EType.PeerAddressChanged, netPeer, remoteEndPoint, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
								}
							}
							flag2 = true;
						}
						this._peersLock.ExitUpgradeableReadLock();
					}
				}
				this.PoolRecycle(packet);
				if (!flag2)
				{
					NetPacket netPacket = this.PoolGetWithProperty(PacketProperty.PeerNotFound, 1);
					netPacket.RawData[1] = 1;
					this.SendRawAndRecycle(netPacket, remoteEndPoint);
				}
			}
			break;
		case PacketProperty.InvalidProtocol:
			if (flag && value2.ConnectionState == ConnectionState.Outgoing)
			{
				this.DisconnectPeerForce(value2, DisconnectReason.InvalidProtocol, SocketError.Success, null);
			}
			break;
		case PacketProperty.Disconnect:
			if (flag)
			{
				DisconnectResult disconnectResult = value2.ProcessDisconnect(packet);
				if (disconnectResult == DisconnectResult.None)
				{
					this.PoolRecycle(packet);
					break;
				}
				this.DisconnectPeerForce(value2, (disconnectResult == DisconnectResult.Disconnect) ? DisconnectReason.RemoteConnectionClose : DisconnectReason.ConnectionRejected, SocketError.Success, packet);
			}
			else
			{
				this.PoolRecycle(packet);
			}
			this.SendRawAndRecycle(this.PoolGetWithProperty(PacketProperty.ShutdownOk), remoteEndPoint);
			break;
		case PacketProperty.ConnectAccept:
			if (flag)
			{
				NetConnectAcceptPacket netConnectAcceptPacket2 = NetConnectAcceptPacket.FromData(packet);
				if (netConnectAcceptPacket2 != null && value2.ProcessConnectAccept(netConnectAcceptPacket2))
				{
					this.CreateEvent(NetEvent.EType.Connect, value2, null, SocketError.Success, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
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
				this.SendRawAndRecycle(this.PoolGetWithProperty(PacketProperty.PeerNotFound), remoteEndPoint);
			}
			break;
		}
	}

	internal void CreateReceiveEvent(NetPacket packet, DeliveryMethod method, byte channelNumber, int headerSize, NetPeer fromPeer)
	{
		if (this.UnsyncedEvents || this.UnsyncedReceiveEvent || this._manualMode)
		{
			NetEvent netEvent;
			lock (this._eventLock)
			{
				netEvent = this._netEventPoolHead;
				if (netEvent == null)
				{
					netEvent = new NetEvent(this);
				}
				else
				{
					this._netEventPoolHead = netEvent.Next;
				}
			}
			netEvent.Next = null;
			netEvent.Type = NetEvent.EType.Receive;
			netEvent.DataReader.SetSource(packet, headerSize);
			netEvent.Peer = fromPeer;
			netEvent.DeliveryMethod = method;
			netEvent.ChannelNumber = channelNumber;
			this.ProcessEvent(netEvent);
			return;
		}
		lock (this._eventLock)
		{
			NetEvent netEvent = this._netEventPoolHead;
			if (netEvent == null)
			{
				netEvent = new NetEvent(this);
			}
			else
			{
				this._netEventPoolHead = netEvent.Next;
			}
			netEvent.Next = null;
			netEvent.Type = NetEvent.EType.Receive;
			netEvent.DataReader.SetSource(packet, headerSize);
			netEvent.Peer = fromPeer;
			netEvent.DeliveryMethod = method;
			netEvent.ChannelNumber = channelNumber;
			if (this._pendingEventTail == null)
			{
				this._pendingEventHead = netEvent;
			}
			else
			{
				this._pendingEventTail.Next = netEvent;
			}
			this._pendingEventTail = netEvent;
		}
	}

	public void SendToAll(NetDataWriter writer, DeliveryMethod options)
	{
		this.SendToAll(writer.Data, 0, writer.Length, options);
	}

	public void SendToAll(byte[] data, DeliveryMethod options)
	{
		this.SendToAll(data, 0, data.Length, options);
	}

	public void SendToAll(byte[] data, int start, int length, DeliveryMethod options)
	{
		this.SendToAll(data, start, length, 0, options);
	}

	public void SendToAll(NetDataWriter writer, byte channelNumber, DeliveryMethod options)
	{
		this.SendToAll(writer.Data, 0, writer.Length, channelNumber, options);
	}

	public void SendToAll(byte[] data, byte channelNumber, DeliveryMethod options)
	{
		this.SendToAll(data, 0, data.Length, channelNumber, options);
	}

	public void SendToAll(byte[] data, int start, int length, byte channelNumber, DeliveryMethod options)
	{
		try
		{
			this._peersLock.EnterReadLock();
			for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				netPeer.Send(data, start, length, channelNumber, options);
			}
		}
		finally
		{
			this._peersLock.ExitReadLock();
		}
	}

	public void SendToAll(NetDataWriter writer, DeliveryMethod options, NetPeer excludePeer)
	{
		this.SendToAll(writer.Data, 0, writer.Length, 0, options, excludePeer);
	}

	public void SendToAll(byte[] data, DeliveryMethod options, NetPeer excludePeer)
	{
		this.SendToAll(data, 0, data.Length, 0, options, excludePeer);
	}

	public void SendToAll(byte[] data, int start, int length, DeliveryMethod options, NetPeer excludePeer)
	{
		this.SendToAll(data, start, length, 0, options, excludePeer);
	}

	public void SendToAll(NetDataWriter writer, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		this.SendToAll(writer.Data, 0, writer.Length, channelNumber, options, excludePeer);
	}

	public void SendToAll(byte[] data, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		this.SendToAll(data, 0, data.Length, channelNumber, options, excludePeer);
	}

	public void SendToAll(byte[] data, int start, int length, byte channelNumber, DeliveryMethod options, NetPeer excludePeer)
	{
		try
		{
			this._peersLock.EnterReadLock();
			for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				if (netPeer != excludePeer)
				{
					netPeer.Send(data, start, length, channelNumber, options);
				}
			}
		}
		finally
		{
			this._peersLock.ExitReadLock();
		}
	}

	public bool Start()
	{
		return this.Start(0);
	}

	public bool Start(IPAddress addressIPv4, IPAddress addressIPv6, int port)
	{
		return this.Start(addressIPv4, addressIPv6, port, manualMode: false);
	}

	public bool Start(string addressIPv4, string addressIPv6, int port)
	{
		IPAddress addressIPv7 = NetUtils.ResolveAddress(addressIPv4);
		IPAddress addressIPv8 = NetUtils.ResolveAddress(addressIPv6);
		return this.Start(addressIPv7, addressIPv8, port);
	}

	public bool Start(int port)
	{
		return this.Start(IPAddress.Any, IPAddress.IPv6Any, port);
	}

	public bool StartInManualMode(IPAddress addressIPv4, IPAddress addressIPv6, int port)
	{
		return this.Start(addressIPv4, addressIPv6, port, manualMode: true);
	}

	public bool StartInManualMode(string addressIPv4, string addressIPv6, int port)
	{
		IPAddress addressIPv7 = NetUtils.ResolveAddress(addressIPv4);
		IPAddress addressIPv8 = NetUtils.ResolveAddress(addressIPv6);
		return this.StartInManualMode(addressIPv7, addressIPv8, port);
	}

	public bool StartInManualMode(int port)
	{
		return this.StartInManualMode(IPAddress.Any, IPAddress.IPv6Any, port);
	}

	public bool SendUnconnectedMessage(byte[] message, IPEndPoint remoteEndPoint)
	{
		return this.SendUnconnectedMessage(message, 0, message.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(NetDataWriter writer, string address, int port)
	{
		IPEndPoint remoteEndPoint = NetUtils.MakeEndPoint(address, port);
		return this.SendUnconnectedMessage(writer.Data, 0, writer.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(NetDataWriter writer, IPEndPoint remoteEndPoint)
	{
		return this.SendUnconnectedMessage(writer.Data, 0, writer.Length, remoteEndPoint);
	}

	public bool SendUnconnectedMessage(byte[] message, int start, int length, IPEndPoint remoteEndPoint)
	{
		NetPacket packet = this.PoolGetWithData(PacketProperty.UnconnectedMessage, message, start, length);
		return this.SendRawAndRecycle(packet, remoteEndPoint) > 0;
	}

	public void TriggerUpdate()
	{
		this._updateTriggerEvent.Set();
	}

	public void PollEvents()
	{
		if (this._manualMode)
		{
			if (this._udpSocketv4 != null)
			{
				this.ManualReceive(this._udpSocketv4, this._bufferEndPointv4);
			}
			if (this._udpSocketv6 != null && this._udpSocketv6 != this._udpSocketv4)
			{
				this.ManualReceive(this._udpSocketv6, this._bufferEndPointv6);
			}
		}
		else if (!this.UnsyncedEvents)
		{
			NetEvent netEvent;
			lock (this._eventLock)
			{
				netEvent = this._pendingEventHead;
				this._pendingEventHead = null;
				this._pendingEventTail = null;
			}
			while (netEvent != null)
			{
				NetEvent next = netEvent.Next;
				this.ProcessEvent(netEvent);
				netEvent = next;
			}
		}
	}

	public NetPeer Connect(string address, int port, string key)
	{
		return this.Connect(address, port, NetDataWriter.FromString(key));
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
			this.CreateEvent(NetEvent.EType.Disconnect, null, null, SocketError.Success, 0, DisconnectReason.UnknownHost, null, DeliveryMethod.Unreliable, 0);
			return null;
		}
		return this.Connect(target, connectionData);
	}

	public NetPeer Connect(IPEndPoint target, string key)
	{
		return this.Connect(target, NetDataWriter.FromString(key));
	}

	public NetPeer Connect(IPEndPoint target, NetDataWriter connectionData)
	{
		if (!this.IsRunning)
		{
			throw new InvalidOperationException("Client is not running");
		}
		lock (this._requestsDict)
		{
			if (this._requestsDict.ContainsKey(target))
			{
				return null;
			}
		}
		byte connectNum = 0;
		this._peersLock.EnterUpgradeableReadLock();
		if (this._peersDict.TryGetValue(target, out var value))
		{
			ConnectionState connectionState = value.ConnectionState;
			if (connectionState == ConnectionState.Outgoing || connectionState == ConnectionState.Connected)
			{
				this._peersLock.ExitUpgradeableReadLock();
				return value;
			}
			connectNum = (byte)((value.ConnectionNum + 1) % 4);
			this.RemovePeer(value);
		}
		value = new NetPeer(this, target, this.GetNextPeerId(), connectNum, connectionData);
		this.AddPeer(value);
		this._peersLock.ExitUpgradeableReadLock();
		return value;
	}

	public void Stop()
	{
		this.Stop(sendDisconnectMessages: true);
	}

	public void Stop(bool sendDisconnectMessages)
	{
		if (this.IsRunning)
		{
			this._pausedSocketFix.Deinitialize();
			this._pausedSocketFix = null;
			for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
			{
				netPeer.Shutdown(null, 0, 0, !sendDisconnectMessages);
			}
			this.CloseSocket();
			this._updateTriggerEvent.Set();
			if (!this._manualMode)
			{
				this._logicThread.Join();
				this._logicThread = null;
			}
			this._peersLock.EnterWriteLock();
			this._headPeer = null;
			this._peersDict.Clear();
			this._peersArray = new NetPeer[32];
			this._peersLock.ExitWriteLock();
			this._peerIds = new ConcurrentQueue<int>();
			this._lastPeerId = 0;
			this._connectedPeersCount = 0;
			this._pendingEventHead = null;
			this._pendingEventTail = null;
		}
	}

	public int GetPeersCount(ConnectionState peerState)
	{
		int num = 0;
		this._peersLock.EnterReadLock();
		for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if ((netPeer.ConnectionState & peerState) != 0)
			{
				num++;
			}
		}
		this._peersLock.ExitReadLock();
		return num;
	}

	public void GetPeersNonAlloc(List<NetPeer> peers, ConnectionState peerState)
	{
		peers.Clear();
		this._peersLock.EnterReadLock();
		for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			if ((netPeer.ConnectionState & peerState) != 0)
			{
				peers.Add(netPeer);
			}
		}
		this._peersLock.ExitReadLock();
	}

	public void DisconnectAll()
	{
		this.DisconnectAll(null, 0, 0);
	}

	public void DisconnectAll(byte[] data, int start, int count)
	{
		this._peersLock.EnterReadLock();
		for (NetPeer netPeer = this._headPeer; netPeer != null; netPeer = netPeer.NextPeer)
		{
			this.DisconnectPeer(netPeer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, force: false, data, start, count, null);
		}
		this._peersLock.ExitReadLock();
	}

	public void DisconnectPeerForce(NetPeer peer)
	{
		this.DisconnectPeerForce(peer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, null);
	}

	public void DisconnectPeer(NetPeer peer)
	{
		this.DisconnectPeer(peer, null, 0, 0);
	}

	public void DisconnectPeer(NetPeer peer, byte[] data)
	{
		this.DisconnectPeer(peer, data, 0, data.Length);
	}

	public void DisconnectPeer(NetPeer peer, NetDataWriter writer)
	{
		this.DisconnectPeer(peer, writer.Data, 0, writer.Length);
	}

	public void DisconnectPeer(NetPeer peer, byte[] data, int start, int count)
	{
		this.DisconnectPeer(peer, DisconnectReason.DisconnectPeerCalled, SocketError.Success, force: false, data, start, count, null);
	}

	public void CreateNtpRequest(IPEndPoint endPoint)
	{
		this._ntpRequests.Add(endPoint, new NtpRequest(endPoint));
	}

	public void CreateNtpRequest(string ntpServerAddress, int port)
	{
		IPEndPoint iPEndPoint = NetUtils.MakeEndPoint(ntpServerAddress, port);
		this._ntpRequests.Add(iPEndPoint, new NtpRequest(iPEndPoint));
	}

	public void CreateNtpRequest(string ntpServerAddress)
	{
		IPEndPoint iPEndPoint = NetUtils.MakeEndPoint(ntpServerAddress, 123);
		this._ntpRequests.Add(iPEndPoint, new NtpRequest(iPEndPoint));
	}

	public NetPeerEnumerator GetEnumerator()
	{
		return new NetPeerEnumerator(this._headPeer);
	}

	IEnumerator<NetPeer> IEnumerable<NetPeer>.GetEnumerator()
	{
		return new NetPeerEnumerator(this._headPeer);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new NetPeerEnumerator(this._headPeer);
	}

	private NetPacket PoolGetWithData(PacketProperty property, byte[] data, int start, int length)
	{
		int headerSize = NetPacket.GetHeaderSize(property);
		NetPacket netPacket = this.PoolGetPacket(length + headerSize);
		netPacket.Property = property;
		Buffer.BlockCopy(data, start, netPacket.RawData, headerSize, length);
		return netPacket;
	}

	private NetPacket PoolGetWithProperty(PacketProperty property, int size)
	{
		NetPacket netPacket = this.PoolGetPacket(size + NetPacket.GetHeaderSize(property));
		netPacket.Property = property;
		return netPacket;
	}

	private NetPacket PoolGetWithProperty(PacketProperty property)
	{
		NetPacket netPacket = this.PoolGetPacket(NetPacket.GetHeaderSize(property));
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
		lock (this._poolLock)
		{
			poolHead = this._poolHead;
			if (poolHead == null)
			{
				return new NetPacket(size);
			}
			this._poolHead = this._poolHead.Next;
			this._poolCount--;
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
		if (packet.RawData.Length > NetConstants.MaxPacketSize || this._poolCount >= this.PacketPoolSize)
		{
			return;
		}
		packet.RawData[0] = 0;
		lock (this._poolLock)
		{
			packet.Next = this._poolHead;
			this._poolHead = packet;
			this._poolCount++;
		}
	}

	static NetManager()
	{
		NetManager.MulticastAddressV6 = IPAddress.Parse("ff02::1");
		NetManager.IPv6Support = Socket.OSSupportsIPv6;
	}

	private void RegisterEndPoint(IPEndPoint ep)
	{
		if (this.UseNativeSockets && ep is NativeEndPoint nativeEndPoint)
		{
			this._nativeAddrMap.Add(new NativeAddr(nativeEndPoint.NativeAddress, nativeEndPoint.NativeAddress.Length), nativeEndPoint);
		}
	}

	private void UnregisterEndPoint(IPEndPoint ep)
	{
		if (this.UseNativeSockets && ep is NativeEndPoint nativeEndPoint)
		{
			NativeAddr key = new NativeAddr(nativeEndPoint.NativeAddress, nativeEndPoint.NativeAddress.Length);
			this._nativeAddrMap.Remove(key);
		}
	}

	private bool ProcessError(SocketException ex)
	{
		switch (ex.SocketErrorCode)
		{
		case SocketError.NotConnected:
			this.NotConnected = true;
			return true;
		case SocketError.OperationAborted:
		case SocketError.Interrupted:
		case SocketError.NotSocket:
			return true;
		default:
			NetDebug.WriteError($"[R]Error code: {(int)ex.SocketErrorCode} - {ex}");
			this.CreateEvent(NetEvent.EType.Error, null, null, ex.SocketErrorCode, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
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
				this.ReceiveFrom(socket, ref bufferEndPoint);
				num++;
				if (num == this.MaxPacketsReceivePerUpdate)
				{
					break;
				}
			}
		}
		catch (SocketException ex)
		{
			this.ProcessError(ex);
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
				return !this.ProcessError(new SocketException((int)socketError));
			}
			return true;
		}
		NativeAddr key = new NativeAddr(addrBuffer, addrSize);
		if (!this._nativeAddrMap.TryGetValue(key, out var value))
		{
			value = new NativeEndPoint(addrBuffer);
		}
		this.OnMessageReceived(packet, value);
		packet = this.PoolGetPacket(NetConstants.MaxPacketSize);
		return true;
	}

	private void NativeReceiveLogic()
	{
		IntPtr handle = this._udpSocketv4.Handle;
		IntPtr s = this._udpSocketv6?.Handle ?? IntPtr.Zero;
		byte[] array = new byte[16];
		byte[] array2 = new byte[28];
		int addrSize = array.Length;
		int addrSize2 = array2.Length;
		List<Socket> list = new List<Socket>(2);
		Socket udpSocketv = this._udpSocketv4;
		Socket udpSocketv2 = this._udpSocketv6;
		NetPacket packet = this.PoolGetPacket(NetConstants.MaxPacketSize);
		while (this.IsRunning)
		{
			try
			{
				if (udpSocketv2 == null)
				{
					this.NativeReceiveFrom(ref packet, handle, array, addrSize);
				}
				bool flag = false;
				if (udpSocketv.Available != 0 || list.Contains(udpSocketv))
				{
					this.NativeReceiveFrom(ref packet, handle, array, addrSize);
					flag = true;
				}
				if (udpSocketv2.Available != 0 || list.Contains(udpSocketv2))
				{
					this.NativeReceiveFrom(ref packet, s, array2, addrSize2);
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
				if (this.ProcessError(ex))
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
		NetPacket netPacket = this.PoolGetPacket(NetConstants.MaxPacketSize);
		netPacket.Size = s.ReceiveFrom(netPacket.RawData, 0, NetConstants.MaxPacketSize, SocketFlags.None, ref bufferEndPoint);
		this.OnMessageReceived(netPacket, (IPEndPoint)bufferEndPoint);
	}

	private void ReceiveLogic()
	{
		EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
		EndPoint bufferEndPoint2 = new IPEndPoint(IPAddress.IPv6Any, 0);
		List<Socket> list = new List<Socket>(2);
		Socket udpSocketv = this._udpSocketv4;
		Socket udpSocketv2 = this._udpSocketv6;
		while (this.IsRunning)
		{
			try
			{
				if (udpSocketv2 == null)
				{
					if (udpSocketv.Available != 0 || udpSocketv.Poll(500000, SelectMode.SelectRead))
					{
						this.ReceiveFrom(udpSocketv, ref bufferEndPoint);
					}
					continue;
				}
				bool flag = false;
				if (udpSocketv.Available != 0 || list.Contains(udpSocketv))
				{
					this.ReceiveFrom(udpSocketv, ref bufferEndPoint);
					flag = true;
				}
				if (udpSocketv2.Available != 0 || list.Contains(udpSocketv2))
				{
					this.ReceiveFrom(udpSocketv2, ref bufferEndPoint2);
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
				if (this.ProcessError(ex))
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
		if (this.IsRunning && !this.NotConnected)
		{
			return false;
		}
		this.NotConnected = false;
		this._manualMode = manualMode;
		this.UseNativeSockets = this.UseNativeSockets && NativeSocket.IsSupported;
		this._udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		if (!this.BindSocket(this._udpSocketv4, new IPEndPoint(addressIPv4, port)))
		{
			return false;
		}
		this.LocalPort = ((IPEndPoint)this._udpSocketv4.LocalEndPoint).Port;
		if (this._pausedSocketFix == null)
		{
			this._pausedSocketFix = new PausedSocketFix(this, addressIPv4, addressIPv6, port, manualMode);
		}
		this.IsRunning = true;
		if (this._manualMode)
		{
			this._bufferEndPointv4 = new IPEndPoint(IPAddress.Any, 0);
		}
		if (NetManager.IPv6Support && this.IPv6Enabled)
		{
			this._udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
			if (this.BindSocket(this._udpSocketv6, new IPEndPoint(addressIPv6, this.LocalPort)))
			{
				if (this._manualMode)
				{
					this._bufferEndPointv6 = new IPEndPoint(IPAddress.IPv6Any, 0);
				}
			}
			else
			{
				this._udpSocketv6 = null;
			}
		}
		if (!manualMode)
		{
			ThreadStart start = ReceiveLogic;
			if (this.UseNativeSockets)
			{
				start = NativeReceiveLogic;
			}
			this._receiveThread = new Thread(start)
			{
				Name = $"ReceiveThread({this.LocalPort})",
				IsBackground = true
			};
			this._receiveThread.Start();
			if (this._logicThread == null)
			{
				this._logicThread = new Thread(UpdateLogic)
				{
					Name = "LogicThread",
					IsBackground = true
				};
				this._logicThread.Start();
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
			socket.ExclusiveAddressUse = !this.ReuseAddress;
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, this.ReuseAddress);
		}
		catch
		{
		}
		if (ep.AddressFamily == AddressFamily.InterNetwork)
		{
			this.Ttl = 255;
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
		int result = this.SendRaw(packet.RawData, 0, packet.Size, remoteEndPoint);
		this.PoolRecycle(packet);
		return result;
	}

	internal int SendRaw(NetPacket packet, IPEndPoint remoteEndPoint)
	{
		return this.SendRaw(packet.RawData, 0, packet.Size, remoteEndPoint);
	}

	internal int SendRaw(byte[] message, int start, int length, IPEndPoint remoteEndPoint)
	{
		if (!this.IsRunning)
		{
			return 0;
		}
		NetPacket netPacket = null;
		if (this._extraPacketLayer != null)
		{
			netPacket = this.PoolGetPacket(length + this._extraPacketLayer.ExtraPacketSizeForLayer);
			Buffer.BlockCopy(message, start, netPacket.RawData, 0, length);
			start = 0;
			this._extraPacketLayer.ProcessOutBoundPacket(ref remoteEndPoint, ref netPacket.RawData, ref start, ref length);
			message = netPacket.RawData;
		}
		Socket socket = this._udpSocketv4;
		if (remoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6 && NetManager.IPv6Support)
		{
			socket = this._udpSocketv6;
			if (socket == null)
			{
				return 0;
			}
		}
		int num2;
		try
		{
			if (this.UseNativeSockets)
			{
				byte[] array;
				if (remoteEndPoint is NativeEndPoint nativeEndPoint)
				{
					array = nativeEndPoint.NativeAddress;
				}
				else
				{
					if (NetManager._endPointBuffer == null)
					{
						NetManager._endPointBuffer = new byte[28];
					}
					array = NetManager._endPointBuffer;
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
					if (NetManager._sendToBuffer == null)
					{
						NetManager._sendToBuffer = new byte[NetConstants.MaxPacketSize];
					}
					Buffer.BlockCopy(message, start, NetManager._sendToBuffer, 0, length);
					message = NetManager._sendToBuffer;
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
				if (this.DisconnectOnUnreachable && this.TryGetPeer(remoteEndPoint, out var peer))
				{
					this.DisconnectPeerForce(peer, (ex.SocketErrorCode == SocketError.HostUnreachable) ? DisconnectReason.HostUnreachable : DisconnectReason.NetworkUnreachable, ex.SocketErrorCode, null);
				}
				this.CreateEvent(NetEvent.EType.Error, null, remoteEndPoint, ex.SocketErrorCode, 0, DisconnectReason.ConnectionFailed, null, DeliveryMethod.Unreliable, 0);
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
				this.PoolRecycle(netPacket);
			}
		}
		if (num2 <= 0)
		{
			return 0;
		}
		if (this.EnableStatistics)
		{
			this.Statistics.IncrementPacketsSent();
			this.Statistics.AddBytesSent(length);
		}
		return num2;
	}

	public bool SendBroadcast(NetDataWriter writer, int port)
	{
		return this.SendBroadcast(writer.Data, 0, writer.Length, port);
	}

	public bool SendBroadcast(byte[] data, int port)
	{
		return this.SendBroadcast(data, 0, data.Length, port);
	}

	public bool SendBroadcast(byte[] data, int start, int length, int port)
	{
		if (!this.IsRunning)
		{
			return false;
		}
		NetPacket netPacket;
		if (this._extraPacketLayer != null)
		{
			int headerSize = NetPacket.GetHeaderSize(PacketProperty.Broadcast);
			netPacket = this.PoolGetPacket(headerSize + length + this._extraPacketLayer.ExtraPacketSizeForLayer);
			netPacket.Property = PacketProperty.Broadcast;
			Buffer.BlockCopy(data, start, netPacket.RawData, headerSize, length);
			int offset = 0;
			int length2 = length + headerSize;
			IPEndPoint endPoint = null;
			this._extraPacketLayer.ProcessOutBoundPacket(ref endPoint, ref netPacket.RawData, ref offset, ref length2);
		}
		else
		{
			netPacket = this.PoolGetWithData(PacketProperty.Broadcast, data, start, length);
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			flag = this._udpSocketv4.SendTo(netPacket.RawData, 0, netPacket.Size, SocketFlags.None, new IPEndPoint(IPAddress.Broadcast, port)) > 0;
			if (this._udpSocketv6 != null)
			{
				flag2 = this._udpSocketv6.SendTo(netPacket.RawData, 0, netPacket.Size, SocketFlags.None, new IPEndPoint(NetManager.MulticastAddressV6, port)) > 0;
			}
		}
		catch (Exception arg)
		{
			NetDebug.WriteError($"[S][MCAST] {arg}");
			return flag;
		}
		finally
		{
			this.PoolRecycle(netPacket);
		}
		return flag || flag2;
	}

	private void CloseSocket()
	{
		this.IsRunning = false;
		this._udpSocketv4?.Close();
		this._udpSocketv6?.Close();
		this._udpSocketv4 = null;
		this._udpSocketv6 = null;
		if (this._receiveThread != null && this._receiveThread != Thread.CurrentThread)
		{
			this._receiveThread.Join();
		}
		this._receiveThread = null;
	}
}

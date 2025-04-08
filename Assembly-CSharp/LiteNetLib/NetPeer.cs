using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using LiteNetLib.Utils;

namespace LiteNetLib
{
	public class NetPeer
	{
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

		public IPEndPoint EndPoint
		{
			get
			{
				return this._remoteEndPoint;
			}
		}

		public ConnectionState ConnectionState
		{
			get
			{
				return this._connectionState;
			}
		}

		internal long ConnectTime
		{
			get
			{
				return this._connectTime;
			}
		}

		public int RemoteId { get; private set; }

		public int Ping
		{
			get
			{
				return this._avgRtt / 2;
			}
		}

		public int RoundTripTime
		{
			get
			{
				return this._avgRtt;
			}
		}

		public int Mtu
		{
			get
			{
				return this._mtu;
			}
		}

		public long RemoteTimeDelta
		{
			get
			{
				return this._remoteDelta;
			}
		}

		public DateTime RemoteUtcTime
		{
			get
			{
				return new DateTime(DateTime.UtcNow.Ticks + this._remoteDelta);
			}
		}

		public int TimeSinceLastPacket
		{
			get
			{
				return this._timeSinceLastPacket;
			}
		}

		internal double ResendDelay
		{
			get
			{
				return this._resendDelay;
			}
		}

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
			this._holdedFragments = new Dictionary<ushort, NetPeer.IncomingFragments>();
			this._deliveredFragments = new Dictionary<ushort, ushort>();
			this._channels = new BaseChannel[(int)(netManager.ChannelsCount * 4)];
			this._channelSendQueue = new ConcurrentQueue<BaseChannel>();
		}

		internal void InitiateEndPointChange()
		{
			this.ResetMtu();
			this._connectionState = ConnectionState.EndPointChange;
		}

		internal void FinishEndPointChange(IPEndPoint newEndPoint)
		{
			if (this._connectionState != ConnectionState.EndPointChange)
			{
				return;
			}
			this._connectionState = ConnectionState.Connected;
			this._remoteEndPoint = newEndPoint;
		}

		internal void ResetMtu()
		{
			this._finishMtu = false;
			if (this.NetManager.MtuOverride > 0)
			{
				this.OverrideMtu(this.NetManager.MtuOverride);
				return;
			}
			if (this.NetManager.UseSafeMtu)
			{
				this.SetMtu(0);
				return;
			}
			this.SetMtu(1);
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
			int num = (int)(channelNumber * 4 + (ordered ? 2 : 0));
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
			return new PooledPacket(netPacket, mtu, (byte)(channelNumber * 4 + deliveryMethod));
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
			Queue<NetPacket> unreliableChannel = this._unreliableChannel;
			lock (unreliableChannel)
			{
				this._unreliableChannel.Enqueue(packet._packet);
			}
		}

		private BaseChannel CreateChannel(byte idx)
		{
			BaseChannel baseChannel = this._channels[(int)idx];
			if (baseChannel != null)
			{
				return baseChannel;
			}
			switch (idx % 4)
			{
			case 0:
				baseChannel = new ReliableChannel(this, false, idx);
				break;
			case 1:
				baseChannel = new SequencedChannel(this, false, idx);
				break;
			case 2:
				baseChannel = new ReliableChannel(this, true, idx);
				break;
			case 3:
				baseChannel = new SequencedChannel(this, true, idx);
				break;
			}
			BaseChannel baseChannel2 = Interlocked.CompareExchange<BaseChannel>(ref this._channels[(int)idx], baseChannel, null);
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
			this.Shutdown(data, start, length, false);
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
			return this._mtu - NetPacket.GetHeaderSize((options == DeliveryMethod.Unreliable) ? PacketProperty.Unreliable : PacketProperty.Channeled);
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
			if (this._connectionState != ConnectionState.Connected || (int)channelNumber >= this._channels.Length)
			{
				return;
			}
			BaseChannel baseChannel = null;
			PacketProperty packetProperty;
			if (deliveryMethod == DeliveryMethod.Unreliable)
			{
				packetProperty = PacketProperty.Unreliable;
			}
			else
			{
				packetProperty = PacketProperty.Channeled;
				baseChannel = this.CreateChannel((byte)(channelNumber * 4 + deliveryMethod));
			}
			int headerSize = NetPacket.GetHeaderSize(packetProperty);
			int mtu = this._mtu;
			if (length + headerSize <= mtu)
			{
				NetPacket netPacket = this.NetManager.PoolGetPacket(headerSize + length);
				netPacket.Property = packetProperty;
				Buffer.BlockCopy(data, start, netPacket.RawData, headerSize, length);
				netPacket.UserData = userData;
				if (baseChannel == null)
				{
					Queue<NetPacket> unreliableChannel = this._unreliableChannel;
					lock (unreliableChannel)
					{
						this._unreliableChannel.Enqueue(netPacket);
						return;
					}
				}
				baseChannel.AddToQueue(netPacket);
				return;
			}
			if (deliveryMethod != DeliveryMethod.ReliableOrdered && deliveryMethod != DeliveryMethod.ReliableUnordered)
			{
				throw new TooBigPacketException("Unreliable or ReliableSequenced packet size exceeded maximum of " + (mtu - headerSize).ToString() + " bytes, Check allowed size by GetMaxSinglePacketSize()");
			}
			int num = mtu - headerSize - 6;
			int num2 = length / num + ((length % num == 0) ? 0 : 1);
			if (num2 > 65535)
			{
				throw new TooBigPacketException("Data was split in " + num2.ToString() + " fragments, which exceeds " + ushort.MaxValue.ToString());
			}
			ushort num3 = (ushort)Interlocked.Increment(ref this._fragmentId);
			ushort num4 = 0;
			while ((int)num4 < num2)
			{
				int num5 = ((length > num) ? num : length);
				NetPacket netPacket2 = this.NetManager.PoolGetPacket(headerSize + num5 + 6);
				netPacket2.Property = packetProperty;
				netPacket2.UserData = userData;
				netPacket2.FragmentId = num3;
				netPacket2.FragmentPart = num4;
				netPacket2.FragmentsTotal = (ushort)num2;
				netPacket2.MarkFragmented();
				Buffer.BlockCopy(data, start + (int)num4 * num, netPacket2.RawData, 10, num5);
				baseChannel.AddToQueue(netPacket2);
				length -= num5;
				num4 += 1;
			}
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
			if ((this._connectionState != ConnectionState.Connected && this._connectionState != ConnectionState.Outgoing) || packet.Size < 9 || BitConverter.ToInt64(packet.RawData, 1) != this._connectTime || packet.ConnectionNumber != this._connectNum)
			{
				return DisconnectResult.None;
			}
			if (this._connectionState != ConnectionState.Connected)
			{
				return DisconnectResult.Reject;
			}
			return DisconnectResult.Disconnect;
		}

		internal void AddToReliableChannelSendQueue(BaseChannel channel)
		{
			this._channelSendQueue.Enqueue(channel);
		}

		internal ShutdownResult Shutdown(byte[] data, int start, int length, bool force)
		{
			object shutdownLock = this._shutdownLock;
			ShutdownResult shutdownResult;
			lock (shutdownLock)
			{
				if (this._connectionState == ConnectionState.Disconnected || this._connectionState == ConnectionState.ShutdownRequested)
				{
					shutdownResult = ShutdownResult.None;
				}
				else
				{
					ShutdownResult shutdownResult2 = ((this._connectionState == ConnectionState.Connected) ? ShutdownResult.WasConnected : ShutdownResult.Success);
					if (force)
					{
						this._connectionState = ConnectionState.Disconnected;
						shutdownResult = shutdownResult2;
					}
					else
					{
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
						shutdownResult = shutdownResult2;
					}
				}
			}
			return shutdownResult;
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
			if (!p.IsFragmented)
			{
				this.NetManager.CreateReceiveEvent(p, method, p.ChannelId / 4, 4, this);
				return;
			}
			ushort fragmentId = p.FragmentId;
			byte channelId = p.ChannelId;
			NetPeer.IncomingFragments incomingFragments;
			if (!this._holdedFragments.TryGetValue(fragmentId, out incomingFragments))
			{
				incomingFragments = new NetPeer.IncomingFragments
				{
					Fragments = new NetPacket[(int)p.FragmentsTotal],
					ChannelId = p.ChannelId
				};
				this._holdedFragments.Add(fragmentId, incomingFragments);
			}
			NetPacket[] fragments = incomingFragments.Fragments;
			if ((int)p.FragmentPart >= fragments.Length || fragments[(int)p.FragmentPart] != null || p.ChannelId != incomingFragments.ChannelId)
			{
				this.NetManager.PoolRecycle(p);
				NetDebug.WriteError("Invalid fragment packet");
				return;
			}
			fragments[(int)p.FragmentPart] = p;
			incomingFragments.ReceivedCount++;
			incomingFragments.TotalSize += p.Size - 10;
			if (incomingFragments.ReceivedCount != fragments.Length)
			{
				return;
			}
			NetPacket netPacket = this.NetManager.PoolGetPacket(incomingFragments.TotalSize);
			int num = 0;
			for (int i = 0; i < incomingFragments.ReceivedCount; i++)
			{
				NetPacket netPacket2 = fragments[i];
				int num2 = netPacket2.Size - 10;
				if (num + num2 > netPacket.RawData.Length)
				{
					this._holdedFragments.Remove(fragmentId);
					NetDebug.WriteError(string.Format("Fragment error pos: {0} >= resultPacketSize: {1} , totalSize: {2}", num + num2, netPacket.RawData.Length, incomingFragments.TotalSize));
					return;
				}
				if (netPacket2.Size > netPacket2.RawData.Length)
				{
					this._holdedFragments.Remove(fragmentId);
					NetDebug.WriteError(string.Format("Fragment error size: {0} > fragment.RawData.Length: {1}", netPacket2.Size, netPacket2.RawData.Length));
					return;
				}
				Buffer.BlockCopy(netPacket2.RawData, 10, netPacket.RawData, num, num2);
				num += num2;
				this.NetManager.PoolRecycle(netPacket2);
				fragments[i] = null;
			}
			this._holdedFragments.Remove(fragmentId);
			this.NetManager.CreateReceiveEvent(netPacket, method, channelId / 4, 0, this);
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
				NetDebug.WriteError(string.Format("[MTU] Broken packet. RMTU {0}, EMTU {1}, PSIZE {2}", num, num2, packet.Size));
				return;
			}
			if (packet.Property == PacketProperty.MtuCheck)
			{
				this._mtuCheckAttempts = 0;
				packet.Property = PacketProperty.MtuOk;
				this.NetManager.SendRawAndRecycle(packet, this._remoteEndPoint);
				return;
			}
			if (num > this._mtu && !this._finishMtu)
			{
				if (num != NetConstants.PossibleMtu[this._mtuIdx + 1] - this.NetManager.ExtraPacketSizeForLayer)
				{
					return;
				}
				object mtuMutex = this._mtuMutex;
				lock (mtuMutex)
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
			object mtuMutex = this._mtuMutex;
			lock (mtuMutex)
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
			ConnectionState connectionState = this._connectionState;
			if (connectionState <= ConnectionState.Connected)
			{
				if (connectionState != ConnectionState.Outgoing)
				{
					if (connectionState == ConnectionState.Connected)
					{
						if (connRequest.ConnectionTime == this._connectTime)
						{
							this.NetManager.SendRaw(this._connectAcceptPacket, this._remoteEndPoint);
						}
						else if (connRequest.ConnectionTime > this._connectTime)
						{
							return ConnectRequestResult.Reconnection;
						}
					}
				}
				else
				{
					if (connRequest.ConnectionTime < this._connectTime)
					{
						return ConnectRequestResult.P2PLose;
					}
					if (connRequest.ConnectionTime == this._connectTime)
					{
						SocketAddress socketAddress = this._remoteEndPoint.Serialize();
						byte[] targetAddress = connRequest.TargetAddress;
						for (int i = socketAddress.Size - 1; i >= 0; i--)
						{
							byte b = socketAddress[i];
							if (b != targetAddress[i] && b < targetAddress[i])
							{
								return ConnectRequestResult.P2PLose;
							}
						}
					}
				}
			}
			else if (connectionState == ConnectionState.ShutdownRequested || connectionState == ConnectionState.Disconnected)
			{
				if (connRequest.ConnectionTime >= this._connectTime)
				{
					return ConnectRequestResult.NewConnection;
				}
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
			case PacketProperty.Unreliable:
				this.NetManager.CreateReceiveEvent(packet, DeliveryMethod.Unreliable, 0, 1, this);
				return;
			case PacketProperty.Channeled:
			case PacketProperty.Ack:
			{
				if ((int)packet.ChannelId > this._channels.Length)
				{
					this.NetManager.PoolRecycle(packet);
					return;
				}
				BaseChannel baseChannel = this._channels[(int)packet.ChannelId] ?? ((packet.Property == PacketProperty.Ack) ? null : this.CreateChannel(packet.ChannelId));
				if (baseChannel != null && !baseChannel.ProcessPacket(packet))
				{
					this.NetManager.PoolRecycle(packet);
					return;
				}
				return;
			}
			case PacketProperty.Ping:
				if (NetUtils.RelativeSequenceNumber((int)packet.Sequence, (int)this._pongPacket.Sequence) > 0)
				{
					FastBitConverter.GetBytes(this._pongPacket.RawData, 3, DateTime.UtcNow.Ticks);
					this._pongPacket.Sequence = packet.Sequence;
					this.NetManager.SendRaw(this._pongPacket, this._remoteEndPoint);
				}
				this.NetManager.PoolRecycle(packet);
				return;
			case PacketProperty.Pong:
				if (packet.Sequence == this._pingPacket.Sequence)
				{
					this._pingTimer.Stop();
					int num = (int)this._pingTimer.ElapsedMilliseconds;
					this._remoteDelta = BitConverter.ToInt64(packet.RawData, 3) + (long)num * 10000L / 2L - DateTime.UtcNow.Ticks;
					this.UpdateRoundTripTime(num);
					this.NetManager.ConnectionLatencyUpdated(this, num / 2);
				}
				this.NetManager.PoolRecycle(packet);
				return;
			case PacketProperty.MtuCheck:
			case PacketProperty.MtuOk:
				this.ProcessMtuPacket(packet);
				return;
			case PacketProperty.Merged:
			{
				int i = 1;
				while (i < packet.Size)
				{
					ushort num2 = BitConverter.ToUInt16(packet.RawData, i);
					if (num2 == 0)
					{
						break;
					}
					i += 2;
					if (packet.RawData.Length - i < (int)num2)
					{
						break;
					}
					NetPacket netPacket = this.NetManager.PoolGetPacket((int)num2);
					Buffer.BlockCopy(packet.RawData, i, netPacket.RawData, 0, (int)num2);
					netPacket.Size = (int)num2;
					if (!netPacket.Verify())
					{
						break;
					}
					i += (int)num2;
					this.ProcessPacket(netPacket);
				}
				this.NetManager.PoolRecycle(packet);
				return;
			}
			}
			NetDebug.WriteError("Error! Unexpected packet type: " + packet.Property.ToString());
		}

		private void SendMerged()
		{
			if (this._mergeCount == 0)
			{
				return;
			}
			int num;
			if (this._mergeCount > 1)
			{
				num = this.NetManager.SendRaw(this._mergeData.RawData, 0, 1 + this._mergePos, this._remoteEndPoint);
			}
			else
			{
				num = this.NetManager.SendRaw(this._mergeData.RawData, 3, this._mergePos - 2, this._remoteEndPoint);
			}
			if (this.NetManager.EnableStatistics)
			{
				this.Statistics.IncrementPacketsSent();
				this.Statistics.AddBytesSent((long)num);
			}
			this._mergePos = 0;
			this._mergeCount = 0;
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
					this.Statistics.AddBytesSent((long)num2);
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
			ConnectionState connectionState = this._connectionState;
			if (connectionState <= ConnectionState.Connected)
			{
				if (connectionState == ConnectionState.Outgoing)
				{
					this._connectTimer += deltaTime;
					if (this._connectTimer > this.NetManager.ReconnectDelay)
					{
						this._connectTimer = 0;
						this._connectAttempts++;
						if (this._connectAttempts > this.NetManager.MaxConnectAttempts)
						{
							this.NetManager.DisconnectPeerForce(this, DisconnectReason.ConnectionFailed, SocketError.Success, null);
							return;
						}
						this.NetManager.SendRaw(this._connectRequestPacket, this._remoteEndPoint);
					}
					return;
				}
				if (connectionState == ConnectionState.Connected)
				{
					if (this._timeSinceLastPacket > this.NetManager.DisconnectTimeout)
					{
						this.NetManager.DisconnectPeerForce(this, DisconnectReason.Timeout, SocketError.Success, null);
						return;
					}
				}
			}
			else if (connectionState != ConnectionState.ShutdownRequested)
			{
				if (connectionState == ConnectionState.Disconnected)
				{
					return;
				}
			}
			else
			{
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
			}
			this._pingSendTimer += deltaTime;
			if (this._pingSendTimer >= this.NetManager.PingInterval)
			{
				this._pingSendTimer = 0;
				NetPacket pingPacket = this._pingPacket;
				ushort sequence = pingPacket.Sequence;
				pingPacket.Sequence = sequence + 1;
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
			BaseChannel baseChannel;
			while (count-- > 0 && this._channelSendQueue.TryDequeue(out baseChannel))
			{
				if (baseChannel.SendAndCheckQueue())
				{
					this._channelSendQueue.Enqueue(baseChannel);
				}
			}
			Queue<NetPacket> unreliableChannel = this._unreliableChannel;
			lock (unreliableChannel)
			{
				int count2 = this._unreliableChannel.Count;
				for (int i = 0; i < count2; i++)
				{
					NetPacket netPacket = this._unreliableChannel.Dequeue();
					this.SendUserData(netPacket);
					this.NetManager.PoolRecycle(netPacket);
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
					ushort num;
					this._deliveredFragments.TryGetValue(packet.FragmentId, out num);
					num += 1;
					if (num == packet.FragmentsTotal)
					{
						this.NetManager.MessageDelivered(this, packet.UserData);
						this._deliveredFragments.Remove(packet.FragmentId);
					}
					else
					{
						this._deliveredFragments[packet.FragmentId] = num;
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

		private readonly Dictionary<ushort, NetPeer.IncomingFragments> _holdedFragments;

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

		private class IncomingFragments
		{
			public NetPacket[] Fragments;

			public int ReceivedCount;

			public int TotalSize;

			public byte ChannelId;
		}
	}
}

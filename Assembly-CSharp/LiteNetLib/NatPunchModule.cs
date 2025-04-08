using System;
using System.Collections.Concurrent;
using System.Net;
using LiteNetLib.Utils;

namespace LiteNetLib
{
	public sealed class NatPunchModule
	{
		internal NatPunchModule(NetManager socket)
		{
			this._socket = socket;
			this._netPacketProcessor.SubscribeReusable<NatPunchModule.NatIntroduceResponsePacket>(new Action<NatPunchModule.NatIntroduceResponsePacket>(this.OnNatIntroductionResponse));
			this._netPacketProcessor.SubscribeReusable<NatPunchModule.NatIntroduceRequestPacket, IPEndPoint>(new Action<NatPunchModule.NatIntroduceRequestPacket, IPEndPoint>(this.OnNatIntroductionRequest));
			this._netPacketProcessor.SubscribeReusable<NatPunchModule.NatPunchPacket, IPEndPoint>(new Action<NatPunchModule.NatPunchPacket, IPEndPoint>(this.OnNatPunch));
		}

		internal void ProcessMessage(IPEndPoint senderEndPoint, NetPacket packet)
		{
			NetDataReader cacheReader = this._cacheReader;
			lock (cacheReader)
			{
				this._cacheReader.SetSource(packet.RawData, 1, packet.Size);
				this._netPacketProcessor.ReadAllPackets(this._cacheReader, senderEndPoint);
			}
		}

		public void Init(INatPunchListener listener)
		{
			this._natPunchListener = listener;
		}

		private void Send<T>(T packet, IPEndPoint target) where T : class, new()
		{
			this._cacheWriter.Reset();
			this._cacheWriter.Put(16);
			this._netPacketProcessor.Write<T>(this._cacheWriter, packet);
			this._socket.SendRaw(this._cacheWriter.Data, 0, this._cacheWriter.Length, target);
		}

		public void NatIntroduce(IPEndPoint hostInternal, IPEndPoint hostExternal, IPEndPoint clientInternal, IPEndPoint clientExternal, string additionalInfo)
		{
			NatPunchModule.NatIntroduceResponsePacket natIntroduceResponsePacket = new NatPunchModule.NatIntroduceResponsePacket
			{
				Token = additionalInfo
			};
			natIntroduceResponsePacket.Internal = hostInternal;
			natIntroduceResponsePacket.External = hostExternal;
			this.Send<NatPunchModule.NatIntroduceResponsePacket>(natIntroduceResponsePacket, clientExternal);
			natIntroduceResponsePacket.Internal = clientInternal;
			natIntroduceResponsePacket.External = clientExternal;
			this.Send<NatPunchModule.NatIntroduceResponsePacket>(natIntroduceResponsePacket, hostExternal);
		}

		public void PollEvents()
		{
			if (this.UnsyncedEvents)
			{
				return;
			}
			if (this._natPunchListener == null || (this._successEvents.IsEmpty && this._requestEvents.IsEmpty))
			{
				return;
			}
			NatPunchModule.SuccessEventData successEventData;
			while (this._successEvents.TryDequeue(out successEventData))
			{
				this._natPunchListener.OnNatIntroductionSuccess(successEventData.TargetEndPoint, successEventData.Type, successEventData.Token);
			}
			NatPunchModule.RequestEventData requestEventData;
			while (this._requestEvents.TryDequeue(out requestEventData))
			{
				this._natPunchListener.OnNatIntroductionRequest(requestEventData.LocalEndPoint, requestEventData.RemoteEndPoint, requestEventData.Token);
			}
		}

		public void SendNatIntroduceRequest(string host, int port, string additionalInfo)
		{
			this.SendNatIntroduceRequest(NetUtils.MakeEndPoint(host, port), additionalInfo);
		}

		public void SendNatIntroduceRequest(IPEndPoint masterServerEndPoint, string additionalInfo)
		{
			string text = NetUtils.GetLocalIp(LocalAddrType.IPv4);
			if (string.IsNullOrEmpty(text))
			{
				text = NetUtils.GetLocalIp(LocalAddrType.IPv6);
			}
			this.Send<NatPunchModule.NatIntroduceRequestPacket>(new NatPunchModule.NatIntroduceRequestPacket
			{
				Internal = NetUtils.MakeEndPoint(text, this._socket.LocalPort),
				Token = additionalInfo
			}, masterServerEndPoint);
		}

		private void OnNatIntroductionRequest(NatPunchModule.NatIntroduceRequestPacket req, IPEndPoint senderEndPoint)
		{
			if (this.UnsyncedEvents)
			{
				this._natPunchListener.OnNatIntroductionRequest(req.Internal, senderEndPoint, req.Token);
				return;
			}
			this._requestEvents.Enqueue(new NatPunchModule.RequestEventData
			{
				LocalEndPoint = req.Internal,
				RemoteEndPoint = senderEndPoint,
				Token = req.Token
			});
		}

		private void OnNatIntroductionResponse(NatPunchModule.NatIntroduceResponsePacket req)
		{
			NatPunchModule.NatPunchPacket natPunchPacket = new NatPunchModule.NatPunchPacket
			{
				Token = req.Token
			};
			this.Send<NatPunchModule.NatPunchPacket>(natPunchPacket, req.Internal);
			this._socket.Ttl = 2;
			this._socket.SendRaw(new byte[] { 17 }, 0, 1, req.External);
			this._socket.Ttl = 255;
			natPunchPacket.IsExternal = true;
			this.Send<NatPunchModule.NatPunchPacket>(natPunchPacket, req.External);
		}

		private void OnNatPunch(NatPunchModule.NatPunchPacket req, IPEndPoint senderEndPoint)
		{
			if (this.UnsyncedEvents)
			{
				this._natPunchListener.OnNatIntroductionSuccess(senderEndPoint, req.IsExternal ? NatAddressType.External : NatAddressType.Internal, req.Token);
				return;
			}
			this._successEvents.Enqueue(new NatPunchModule.SuccessEventData
			{
				TargetEndPoint = senderEndPoint,
				Type = (req.IsExternal ? NatAddressType.External : NatAddressType.Internal),
				Token = req.Token
			});
		}

		private readonly NetManager _socket;

		private readonly ConcurrentQueue<NatPunchModule.RequestEventData> _requestEvents = new ConcurrentQueue<NatPunchModule.RequestEventData>();

		private readonly ConcurrentQueue<NatPunchModule.SuccessEventData> _successEvents = new ConcurrentQueue<NatPunchModule.SuccessEventData>();

		private readonly NetDataReader _cacheReader = new NetDataReader();

		private readonly NetDataWriter _cacheWriter = new NetDataWriter();

		private readonly NetPacketProcessor _netPacketProcessor = new NetPacketProcessor(256);

		private INatPunchListener _natPunchListener;

		public const int MaxTokenLength = 256;

		public bool UnsyncedEvents;

		private struct RequestEventData
		{
			public IPEndPoint LocalEndPoint;

			public IPEndPoint RemoteEndPoint;

			public string Token;
		}

		private struct SuccessEventData
		{
			public IPEndPoint TargetEndPoint;

			public NatAddressType Type;

			public string Token;
		}

		private class NatIntroduceRequestPacket
		{
			public IPEndPoint Internal { get; set; }

			public string Token { get; set; }
		}

		private class NatIntroduceResponsePacket
		{
			public IPEndPoint Internal { get; set; }

			public IPEndPoint External { get; set; }

			public string Token { get; set; }
		}

		private class NatPunchPacket
		{
			public string Token { get; set; }

			public bool IsExternal { get; set; }
		}
	}
}

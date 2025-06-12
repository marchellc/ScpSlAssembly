using System.Collections.Concurrent;
using System.Net;
using LiteNetLib.Utils;

namespace LiteNetLib;

public sealed class NatPunchModule
{
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

	private readonly NetManager _socket;

	private readonly ConcurrentQueue<RequestEventData> _requestEvents = new ConcurrentQueue<RequestEventData>();

	private readonly ConcurrentQueue<SuccessEventData> _successEvents = new ConcurrentQueue<SuccessEventData>();

	private readonly NetDataReader _cacheReader = new NetDataReader();

	private readonly NetDataWriter _cacheWriter = new NetDataWriter();

	private readonly NetPacketProcessor _netPacketProcessor = new NetPacketProcessor(256);

	private INatPunchListener _natPunchListener;

	public const int MaxTokenLength = 256;

	public bool UnsyncedEvents;

	internal NatPunchModule(NetManager socket)
	{
		this._socket = socket;
		this._netPacketProcessor.SubscribeReusable<NatIntroduceResponsePacket>(OnNatIntroductionResponse);
		this._netPacketProcessor.SubscribeReusable<NatIntroduceRequestPacket, IPEndPoint>(OnNatIntroductionRequest);
		this._netPacketProcessor.SubscribeReusable<NatPunchPacket, IPEndPoint>(OnNatPunch);
	}

	internal void ProcessMessage(IPEndPoint senderEndPoint, NetPacket packet)
	{
		lock (this._cacheReader)
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
		this._cacheWriter.Put((byte)16);
		this._netPacketProcessor.Write(this._cacheWriter, packet);
		this._socket.SendRaw(this._cacheWriter.Data, 0, this._cacheWriter.Length, target);
	}

	public void NatIntroduce(IPEndPoint hostInternal, IPEndPoint hostExternal, IPEndPoint clientInternal, IPEndPoint clientExternal, string additionalInfo)
	{
		NatIntroduceResponsePacket natIntroduceResponsePacket = new NatIntroduceResponsePacket
		{
			Token = additionalInfo
		};
		natIntroduceResponsePacket.Internal = hostInternal;
		natIntroduceResponsePacket.External = hostExternal;
		this.Send(natIntroduceResponsePacket, clientExternal);
		natIntroduceResponsePacket.Internal = clientInternal;
		natIntroduceResponsePacket.External = clientExternal;
		this.Send(natIntroduceResponsePacket, hostExternal);
	}

	public void PollEvents()
	{
		if (!this.UnsyncedEvents && this._natPunchListener != null && (!this._successEvents.IsEmpty || !this._requestEvents.IsEmpty))
		{
			SuccessEventData result;
			while (this._successEvents.TryDequeue(out result))
			{
				this._natPunchListener.OnNatIntroductionSuccess(result.TargetEndPoint, result.Type, result.Token);
			}
			RequestEventData result2;
			while (this._requestEvents.TryDequeue(out result2))
			{
				this._natPunchListener.OnNatIntroductionRequest(result2.LocalEndPoint, result2.RemoteEndPoint, result2.Token);
			}
		}
	}

	public void SendNatIntroduceRequest(string host, int port, string additionalInfo)
	{
		this.SendNatIntroduceRequest(NetUtils.MakeEndPoint(host, port), additionalInfo);
	}

	public void SendNatIntroduceRequest(IPEndPoint masterServerEndPoint, string additionalInfo)
	{
		string localIp = NetUtils.GetLocalIp(LocalAddrType.IPv4);
		if (string.IsNullOrEmpty(localIp))
		{
			localIp = NetUtils.GetLocalIp(LocalAddrType.IPv6);
		}
		this.Send(new NatIntroduceRequestPacket
		{
			Internal = NetUtils.MakeEndPoint(localIp, this._socket.LocalPort),
			Token = additionalInfo
		}, masterServerEndPoint);
	}

	private void OnNatIntroductionRequest(NatIntroduceRequestPacket req, IPEndPoint senderEndPoint)
	{
		if (this.UnsyncedEvents)
		{
			this._natPunchListener.OnNatIntroductionRequest(req.Internal, senderEndPoint, req.Token);
			return;
		}
		this._requestEvents.Enqueue(new RequestEventData
		{
			LocalEndPoint = req.Internal,
			RemoteEndPoint = senderEndPoint,
			Token = req.Token
		});
	}

	private void OnNatIntroductionResponse(NatIntroduceResponsePacket req)
	{
		NatPunchPacket natPunchPacket = new NatPunchPacket
		{
			Token = req.Token
		};
		this.Send(natPunchPacket, req.Internal);
		this._socket.Ttl = 2;
		this._socket.SendRaw(new byte[1] { 17 }, 0, 1, req.External);
		this._socket.Ttl = 255;
		natPunchPacket.IsExternal = true;
		this.Send(natPunchPacket, req.External);
	}

	private void OnNatPunch(NatPunchPacket req, IPEndPoint senderEndPoint)
	{
		if (this.UnsyncedEvents)
		{
			this._natPunchListener.OnNatIntroductionSuccess(senderEndPoint, req.IsExternal ? NatAddressType.External : NatAddressType.Internal, req.Token);
			return;
		}
		this._successEvents.Enqueue(new SuccessEventData
		{
			TargetEndPoint = senderEndPoint,
			Type = (req.IsExternal ? NatAddressType.External : NatAddressType.Internal),
			Token = req.Token
		});
	}
}

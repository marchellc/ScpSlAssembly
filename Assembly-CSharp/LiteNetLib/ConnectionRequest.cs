using System;
using System.Net;
using System.Threading;
using LiteNetLib.Utils;

namespace LiteNetLib
{
	public class ConnectionRequest
	{
		public NetDataReader Data
		{
			get
			{
				return this.InternalPacket.Data;
			}
		}

		internal ConnectionRequestResult Result { get; private set; }

		internal void UpdateRequest(NetConnectRequestPacket connectRequest)
		{
			if (connectRequest.ConnectionTime < this.InternalPacket.ConnectionTime)
			{
				return;
			}
			if (connectRequest.ConnectionTime == this.InternalPacket.ConnectionTime && connectRequest.ConnectionNumber == this.InternalPacket.ConnectionNumber)
			{
				return;
			}
			this.InternalPacket = connectRequest;
		}

		private bool TryActivate()
		{
			return Interlocked.CompareExchange(ref this._used, 1, 0) == 0;
		}

		internal ConnectionRequest(IPEndPoint remoteEndPoint, NetConnectRequestPacket requestPacket, NetManager listener)
		{
			this.InternalPacket = requestPacket;
			this.RemoteEndPoint = remoteEndPoint;
			this._listener = listener;
		}

		public NetPeer AcceptIfKey(string key)
		{
			if (!this.TryActivate())
			{
				return null;
			}
			try
			{
				if (this.Data.GetString() == key)
				{
					this.Result = ConnectionRequestResult.Accept;
				}
			}
			catch
			{
				NetDebug.WriteError("[AC] Invalid incoming data");
			}
			if (this.Result == ConnectionRequestResult.Accept)
			{
				return this._listener.OnConnectionSolved(this, null, 0, 0);
			}
			this.Result = ConnectionRequestResult.Reject;
			this._listener.OnConnectionSolved(this, null, 0, 0);
			return null;
		}

		public NetPeer Accept()
		{
			if (!this.TryActivate())
			{
				return null;
			}
			this.Result = ConnectionRequestResult.Accept;
			return this._listener.OnConnectionSolved(this, null, 0, 0);
		}

		public void Reject(byte[] rejectData, int start, int length, bool force)
		{
			if (!this.TryActivate())
			{
				return;
			}
			this.Result = (force ? ConnectionRequestResult.RejectForce : ConnectionRequestResult.Reject);
			this._listener.OnConnectionSolved(this, rejectData, start, length);
		}

		public void Reject(byte[] rejectData, int start, int length)
		{
			this.Reject(rejectData, start, length, false);
		}

		public void RejectForce(byte[] rejectData, int start, int length)
		{
			this.Reject(rejectData, start, length, true);
		}

		public void RejectForce()
		{
			this.Reject(null, 0, 0, true);
		}

		public void RejectForce(byte[] rejectData)
		{
			this.Reject(rejectData, 0, rejectData.Length, true);
		}

		public void RejectForce(NetDataWriter rejectData)
		{
			this.Reject(rejectData.Data, 0, rejectData.Length, true);
		}

		public void Reject()
		{
			this.Reject(null, 0, 0, false);
		}

		public void Reject(byte[] rejectData)
		{
			this.Reject(rejectData, 0, rejectData.Length, false);
		}

		public void Reject(NetDataWriter rejectData)
		{
			this.Reject(rejectData.Data, 0, rejectData.Length, false);
		}

		private readonly NetManager _listener;

		private int _used;

		internal NetConnectRequestPacket InternalPacket;

		public readonly IPEndPoint RemoteEndPoint;
	}
}

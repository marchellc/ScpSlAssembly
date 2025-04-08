using System;
using System.Net;
using System.Net.Sockets;

namespace LiteNetLib.Utils
{
	internal sealed class NtpRequest
	{
		public NtpRequest(IPEndPoint endPoint)
		{
			this._ntpEndPoint = endPoint;
		}

		public bool NeedToKill
		{
			get
			{
				return this._killTime >= 10000;
			}
		}

		public bool Send(Socket socket, int time)
		{
			this._resendTime += time;
			this._killTime += time;
			if (this._resendTime < 1000)
			{
				return false;
			}
			NtpPacket ntpPacket = new NtpPacket();
			bool flag;
			try
			{
				flag = socket.SendTo(ntpPacket.Bytes, 0, ntpPacket.Bytes.Length, SocketFlags.None, this._ntpEndPoint) == ntpPacket.Bytes.Length;
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		private const int ResendTimer = 1000;

		private const int KillTimer = 10000;

		public const int DefaultPort = 123;

		private readonly IPEndPoint _ntpEndPoint;

		private int _resendTime = 1000;

		private int _killTime;
	}
}

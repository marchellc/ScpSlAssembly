using System;
using Mirror;

namespace NetworkManagerUtils
{
	public class DummyNetworkConnection : NetworkConnectionToClient
	{
		public DummyNetworkConnection()
			: base(DummyNetworkConnection._idGenerator--)
		{
		}

		public override string address { get; } = "127.0.0.1";

		public override void Send(ArraySegment<byte> segment, int channelId = 0)
		{
		}

		protected override void SendToTransport(ArraySegment<byte> segment, int channelId = 0)
		{
		}

		private const string DummyAddress = "127.0.0.1";

		private static int _idGenerator = 65535;
	}
}

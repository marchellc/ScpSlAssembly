using System;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class DeviceEventArgs : EventArgs
	{
		public DeviceEventArgs(NatDevice device)
		{
			this.Device = device;
		}

		public NatDevice Device { get; private set; }
	}
}

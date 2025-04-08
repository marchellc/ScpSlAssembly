using System;
using System.Net;
using UnityEngine;

namespace LiteNetLib
{
	public class PausedSocketFix
	{
		public PausedSocketFix(NetManager netManager, IPAddress ipv4, IPAddress ipv6, int port, bool manualMode)
		{
			this._netManager = netManager;
			this._ipv4 = ipv4;
			this._ipv6 = ipv6;
			this._port = port;
			this._manualMode = manualMode;
			Application.focusChanged += this.Application_focusChanged;
			this._initialized = true;
		}

		public void Deinitialize()
		{
			if (this._initialized)
			{
				Application.focusChanged -= this.Application_focusChanged;
			}
			this._initialized = false;
		}

		private void Application_focusChanged(bool focused)
		{
			if (focused)
			{
				if (!this._initialized)
				{
					return;
				}
				if (!this._netManager.IsRunning)
				{
					return;
				}
				if (!this._netManager.NotConnected)
				{
					return;
				}
				if (!this._netManager.Start(this._ipv4, this._ipv6, this._port, this._manualMode))
				{
					NetDebug.WriteError(string.Format("[S] Cannot restore connection. Ipv4 {0}, Ipv6 {1}, Port {2}, ManualMode {3}", new object[] { this._ipv4, this._ipv6, this._port, this._manualMode }));
				}
			}
		}

		private readonly NetManager _netManager;

		private readonly IPAddress _ipv4;

		private readonly IPAddress _ipv6;

		private readonly int _port;

		private readonly bool _manualMode;

		private bool _initialized;
	}
}

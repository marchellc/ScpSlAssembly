using System.Net;
using UnityEngine;

namespace LiteNetLib;

public class PausedSocketFix
{
	private readonly NetManager _netManager;

	private readonly IPAddress _ipv4;

	private readonly IPAddress _ipv6;

	private readonly int _port;

	private readonly bool _manualMode;

	private bool _initialized;

	public PausedSocketFix(NetManager netManager, IPAddress ipv4, IPAddress ipv6, int port, bool manualMode)
	{
		this._netManager = netManager;
		this._ipv4 = ipv4;
		this._ipv6 = ipv6;
		this._port = port;
		this._manualMode = manualMode;
		Application.focusChanged += Application_focusChanged;
		this._initialized = true;
	}

	public void Deinitialize()
	{
		if (this._initialized)
		{
			Application.focusChanged -= Application_focusChanged;
		}
		this._initialized = false;
	}

	private void Application_focusChanged(bool focused)
	{
		if (focused && this._initialized && this._netManager.IsRunning && this._netManager.NotConnected && !this._netManager.Start(this._ipv4, this._ipv6, this._port, this._manualMode))
		{
			NetDebug.WriteError($"[S] Cannot restore connection. Ipv4 {this._ipv4}, Ipv6 {this._ipv6}, Port {this._port}, ManualMode {this._manualMode}");
		}
	}
}

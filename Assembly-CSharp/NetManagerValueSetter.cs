using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;

public class NetManagerValueSetter : MonoBehaviour
{
	private CustomNetworkManager _singleton;

	private void Start()
	{
		this._singleton = NetworkManager.singleton.GetComponent<CustomNetworkManager>();
	}

	public void ChangeIP(string ip)
	{
		this._singleton.networkAddress = ip;
		CustomNetworkManager.ConnectionIp = ip;
	}

	public void ChangePort(ushort port)
	{
		LiteNetLib4MirrorTransport.Singleton.port = port;
	}

	public void JoinGame()
	{
		this._singleton.StartClient();
	}

	public void HostGame()
	{
		this._singleton.StartHost();
	}

	public void Disconnect()
	{
		this._singleton.StopHost();
	}
}

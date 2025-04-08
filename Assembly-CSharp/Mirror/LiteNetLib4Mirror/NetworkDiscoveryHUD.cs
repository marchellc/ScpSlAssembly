using System;
using System.Collections;
using System.ComponentModel;
using System.Net;
using UnityEngine;
using UnityEngine.Events;

namespace Mirror.LiteNetLib4Mirror
{
	[RequireComponent(typeof(NetworkManager))]
	[RequireComponent(typeof(NetworkManagerHUD))]
	[RequireComponent(typeof(LiteNetLib4MirrorTransport))]
	[RequireComponent(typeof(LiteNetLib4MirrorDiscovery))]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class NetworkDiscoveryHUD : MonoBehaviour
	{
		private void Awake()
		{
			this._managerHud = base.GetComponent<NetworkManagerHUD>();
		}

		private IEnumerator StartDiscovery()
		{
			this._noDiscovering = false;
			LiteNetLib4MirrorDiscovery.InitializeFinder();
			LiteNetLib4MirrorDiscovery.Singleton.onDiscoveryResponse.AddListener(new UnityAction<IPEndPoint, string>(this.OnClientDiscoveryResponse));
			while (!this._noDiscovering)
			{
				LiteNetLib4MirrorDiscovery.SendDiscoveryRequest("NetworkManagerHUD");
				yield return new WaitForSeconds(this.discoveryInterval);
			}
			LiteNetLib4MirrorDiscovery.Singleton.onDiscoveryResponse.RemoveListener(new UnityAction<IPEndPoint, string>(this.OnClientDiscoveryResponse));
			LiteNetLib4MirrorDiscovery.StopDiscovery();
			yield break;
		}

		private void OnClientDiscoveryResponse(IPEndPoint endpoint, string text)
		{
			string text2 = endpoint.Address.ToString();
			NetworkManager.singleton.networkAddress = text2;
			NetworkManager.singleton.maxConnections = 2;
			LiteNetLib4MirrorTransport.Singleton.clientAddress = text2;
			LiteNetLib4MirrorTransport.Singleton.port = (ushort)endpoint.Port;
			LiteNetLib4MirrorTransport.Singleton.maxConnections = 2;
			NetworkManager.singleton.StartClient();
			this._noDiscovering = true;
		}

		[SerializeField]
		public float discoveryInterval = 1f;

		private NetworkManagerHUD _managerHud;

		private bool _noDiscovering = true;
	}
}

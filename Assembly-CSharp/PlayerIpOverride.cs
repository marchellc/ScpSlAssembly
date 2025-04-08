using System;
using Mirror;
using Mirror.LiteNetLib4Mirror;

public class PlayerIpOverride : NetworkBehaviour
{
	private void Start()
	{
		if (CustomLiteNetLib4MirrorTransport.IpPassthroughEnabled && NetworkServer.active && !base.isLocalPlayer)
		{
			NetworkConnectionToClient connectionToClient = base.connectionToClient;
			if (connectionToClient != null)
			{
				try
				{
					int id = LiteNetLib4MirrorServer.Peers[connectionToClient.connectionId].Id;
					if (CustomLiteNetLib4MirrorTransport.RealIpAddresses.ContainsKey(id))
					{
						connectionToClient.IpOverride = CustomLiteNetLib4MirrorTransport.RealIpAddresses[id];
					}
				}
				catch (Exception ex)
				{
					ServerConsole.AddLog("Error during IP passthrough processing: " + ex.Message, ConsoleColor.Gray, false);
				}
				return;
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}
}

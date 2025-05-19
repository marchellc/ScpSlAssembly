using System.Runtime.InteropServices;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies;

public class ZombieConfirmationBox : MonoBehaviour
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct ScpReviveBlockMessage : NetworkMessage
	{
	}

	private static void ServerReceiveMessage(NetworkConnection conn)
	{
		if (NetworkServer.active && ReferenceHub.TryGetHubNetID(conn.identity.netId, out var hub) && Scp049ResurrectAbility.GetResurrectionsNumber(hub) != 0)
		{
			Scp049ResurrectAbility.RegisterPlayerResurrection(hub, 2);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += delegate
		{
			NetworkServer.ReplaceHandler(delegate(NetworkConnectionToClient conn, ScpReviveBlockMessage msg)
			{
				ServerReceiveMessage(conn);
			});
		};
	}
}

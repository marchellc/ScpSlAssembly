using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049.Zombies
{
	public class ZombieConfirmationBox : MonoBehaviour
	{
		private static void ServerReceiveMessage(NetworkConnection conn)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetHubNetID(conn.identity.netId, out referenceHub))
			{
				return;
			}
			if (Scp049ResurrectAbility.GetResurrectionsNumber(referenceHub) == 0)
			{
				return;
			}
			Scp049ResurrectAbility.RegisterPlayerResurrection(referenceHub, 2);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkServer.ReplaceHandler<ZombieConfirmationBox.ScpReviveBlockMessage>(delegate(NetworkConnectionToClient conn, ZombieConfirmationBox.ScpReviveBlockMessage msg)
				{
					ZombieConfirmationBox.ServerReceiveMessage(conn);
				}, true);
			};
		}

		public struct ScpReviveBlockMessage : NetworkMessage
		{
		}
	}
}

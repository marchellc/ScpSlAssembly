using System;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Firearms.BasicMessages
{
	public static class DamageIndicatorMessageProcessor
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += DamageIndicatorMessageProcessor.RegisterHandlers;
		}

		private static void RegisterHandlers()
		{
			NetworkClient.ReplaceHandler<DamageIndicatorMessage>(delegate(DamageIndicatorMessage msg)
			{
				DamageIndicator.ReceiveDamageFromPosition(msg.DamagePosition.Position, (float)msg.ReceivedDamage);
			}, true);
		}
	}
}

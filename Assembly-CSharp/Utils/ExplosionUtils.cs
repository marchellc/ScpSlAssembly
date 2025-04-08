using System;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace Utils
{
	public static class ExplosionUtils
	{
		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += ExplosionUtils.ReplaceHandler;
		}

		private static void ReplaceHandler()
		{
			NetworkClient.ReplaceHandler<ExplosionUtils.GrenadeExplosionMessage>(delegate(ExplosionUtils.GrenadeExplosionMessage msg)
			{
			}, true);
		}

		public static void ServerExplode(ReferenceHub hub, ExplosionType explosionType)
		{
			ExplosionUtils.ServerExplode(hub.transform.position, new Footprint(hub), explosionType);
		}

		public static void ServerExplode(Vector3 position, Footprint footprint, ExplosionType explosionType)
		{
			ThrowableItem throwableItem;
			if (!InventoryItemLoader.TryGetItem<ThrowableItem>(ItemType.GrenadeHE, out throwableItem))
			{
				return;
			}
			ExplosionGrenade explosionGrenade = throwableItem.Projectile as ExplosionGrenade;
			if (explosionGrenade == null)
			{
				return;
			}
			ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
			ExplosionGrenade.Explode(footprint, position, explosionGrenade, explosionType);
		}

		public static void ServerSpawnEffect(Vector3 pos, ItemType targetEffectGrenade)
		{
			new ExplosionUtils.GrenadeExplosionMessage
			{
				GrenadeType = (byte)targetEffectGrenade,
				Pos = new RelativePosition(pos)
			}.SendToAuthenticated(0);
		}

		public struct GrenadeExplosionMessage : NetworkMessage
		{
			public byte GrenadeType;

			public RelativePosition Pos;
		}
	}
}

using System;
using Footprinting;
using InventorySystem;
using InventorySystem.Items.ThrowableProjectiles;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace Utils;

public static class ExplosionUtils
{
	public struct GrenadeExplosionMessage : NetworkMessage
	{
		public byte GrenadeType;

		public RelativePosition Pos;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientReady += ReplaceHandler;
	}

	private static void ReplaceHandler()
	{
		NetworkClient.ReplaceHandler((Action<GrenadeExplosionMessage>)delegate
		{
		}, requireAuthentication: true);
	}

	public static void ServerExplode(ReferenceHub hub, ExplosionType explosionType)
	{
		ExplosionUtils.ServerExplode(hub.transform.position, new Footprint(hub), explosionType);
	}

	public static void ServerExplode(Vector3 position, Footprint footprint, ExplosionType explosionType)
	{
		if (InventoryItemLoader.TryGetItem<ThrowableItem>(ItemType.GrenadeHE, out var result) && result.Projectile is ExplosionGrenade settingsReference)
		{
			ExplosionUtils.ServerSpawnEffect(position, ItemType.GrenadeHE);
			ExplosionGrenade.Explode(footprint, position, settingsReference, explosionType);
		}
	}

	public static void ServerSpawnEffect(Vector3 pos, ItemType targetEffectGrenade)
	{
		new GrenadeExplosionMessage
		{
			GrenadeType = (byte)targetEffectGrenade,
			Pos = new RelativePosition(pos)
		}.SendToAuthenticated();
	}
}

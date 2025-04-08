using System;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

public class Hitmarker : MonoBehaviour
{
	public static void PlayHitmarker(float size, bool playAudio = true)
	{
	}

	public static void SendHitmarkerDirectly(ReferenceHub hub, float size, bool playAudio = true)
	{
		if (hub == null)
		{
			return;
		}
		size = Mathf.Clamp(size, 0f, 2.55f);
		if (hub.isLocalPlayer)
		{
			Hitmarker.PlayHitmarker(size, playAudio);
			return;
		}
		new Hitmarker.HitmarkerMessage((byte)Mathf.RoundToInt(size / 2.55f * 255f), playAudio).SendToSpectatorsOf(hub, true);
	}

	public static void SendHitmarkerDirectly(NetworkConnection conn, float size)
	{
		ReferenceHub referenceHub;
		if (!ReferenceHub.TryGetHub(conn, out referenceHub))
		{
			return;
		}
		Hitmarker.SendHitmarkerDirectly(referenceHub, size, true);
	}

	public static void SendHitmarkerConditionally(float size, AttackerDamageHandler adh, ReferenceHub victim)
	{
		if (Hitmarker.CheckHitmarkerPerms(adh, victim))
		{
			Hitmarker.SendHitmarkerDirectly(adh.Attacker.Hub, size, true);
		}
	}

	public static bool CheckHitmarkerPerms(AttackerDamageHandler adh, ReferenceHub victim)
	{
		IHitmarkerPreventer hitmarkerPreventer = victim.roleManager.CurrentRole as IHitmarkerPreventer;
		if (hitmarkerPreventer != null && hitmarkerPreventer.TryPreventHitmarker(adh))
		{
			return false;
		}
		PlayerEffectsController playerEffectsController = victim.playerEffectsController;
		for (int i = 0; i < playerEffectsController.EffectsLength; i++)
		{
			StatusEffectBase statusEffectBase = playerEffectsController.AllEffects[i];
			IHitmarkerPreventer hitmarkerPreventer2 = statusEffectBase as IHitmarkerPreventer;
			if (hitmarkerPreventer2 != null && statusEffectBase.IsEnabled && hitmarkerPreventer2.TryPreventHitmarker(adh))
			{
				return false;
			}
		}
		return true;
	}

	private const float MaxSize = 2.55f;

	public struct HitmarkerMessage : NetworkMessage
	{
		public HitmarkerMessage(byte size, bool playAudio = true)
		{
			this.Size = size;
			this.Audio = playAudio;
		}

		public byte Size;

		public bool Audio;
	}
}

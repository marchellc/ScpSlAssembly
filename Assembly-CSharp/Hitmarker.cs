using CustomPlayerEffects;
using Mirror;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using UnityEngine;

public class Hitmarker : MonoBehaviour
{
	public struct HitmarkerMessage : NetworkMessage
	{
		public byte Size;

		public bool Audio;

		public HitmarkerMessage(byte size, bool playAudio = true)
		{
			this.Size = size;
			this.Audio = playAudio;
		}
	}

	private const float MaxSize = 2.55f;

	public static void PlayHitmarker(float size, bool playAudio = true)
	{
	}

	public static void SendHitmarkerDirectly(ReferenceHub hub, float size, bool playAudio = true)
	{
		if (!(hub == null))
		{
			size = Mathf.Clamp(size, 0f, 2.55f);
			if (hub.isLocalPlayer)
			{
				Hitmarker.PlayHitmarker(size, playAudio);
			}
			else
			{
				new HitmarkerMessage((byte)Mathf.RoundToInt(size / 2.55f * 255f), playAudio).SendToSpectatorsOf(hub, includeTarget: true);
			}
		}
	}

	public static void SendHitmarkerDirectly(NetworkConnection conn, float size)
	{
		if (ReferenceHub.TryGetHub(conn, out var hub))
		{
			Hitmarker.SendHitmarkerDirectly(hub, size);
		}
	}

	public static void SendHitmarkerConditionally(float size, AttackerDamageHandler adh, ReferenceHub victim)
	{
		if (Hitmarker.CheckHitmarkerPerms(adh, victim))
		{
			Hitmarker.SendHitmarkerDirectly(adh.Attacker.Hub, size);
		}
	}

	public static bool CheckHitmarkerPerms(AttackerDamageHandler adh, ReferenceHub victim)
	{
		if (victim.roleManager.CurrentRole is IHitmarkerPreventer hitmarkerPreventer && hitmarkerPreventer.TryPreventHitmarker(adh))
		{
			return false;
		}
		PlayerEffectsController playerEffectsController = victim.playerEffectsController;
		for (int i = 0; i < playerEffectsController.EffectsLength; i++)
		{
			StatusEffectBase statusEffectBase = playerEffectsController.AllEffects[i];
			if (statusEffectBase is IHitmarkerPreventer hitmarkerPreventer2 && statusEffectBase.IsEnabled && hitmarkerPreventer2.TryPreventHitmarker(adh))
			{
				return false;
			}
		}
		return true;
	}
}

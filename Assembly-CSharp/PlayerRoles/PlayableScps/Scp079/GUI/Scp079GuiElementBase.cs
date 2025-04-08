using System;
using AudioPooling;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079GuiElementBase : MonoBehaviour
	{
		private protected Scp079Role Role { protected get; private set; }

		private protected ReferenceHub Owner { protected get; private set; }

		internal virtual void Init(Scp079Role role, ReferenceHub owner)
		{
			this.Role = role;
			this.Owner = owner;
		}

		protected PooledAudioSource PlaySound(AudioClip clip, float pitch = 1f)
		{
			return AudioSourcePoolManager.Play2DWithParent(clip, MainCameraController.CurrentCamera, 1f, MixerChannel.DefaultSfx, pitch);
		}
	}
}

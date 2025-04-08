using System;
using AudioPooling;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps
{
	public class CooldownAudio : MonoBehaviour
	{
		public void PlayAudio()
		{
			if (NetworkTime.time < this._lastTime)
			{
				return;
			}
			this._lastTime = NetworkTime.time + this.Cooldown;
			AudioSourcePoolManager.Play2DWithParent(this._cooldownAudio, this._player.transform, 1f, MixerChannel.DefaultSfx, 1f);
		}

		public double Cooldown;

		[SerializeField]
		private AudioClip _cooldownAudio;

		[SerializeField]
		private PlayerRoleBase _player;

		private double _lastTime;
	}
}

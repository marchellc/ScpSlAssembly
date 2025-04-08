using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class Scanned : StatusEffectBase, ISoundtrackMutingEffect
	{
		public bool MuteSoundtrack
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			this.UpdateSourceMute();
			this._soundSource.Play();
		}

		protected override void Disabled()
		{
			base.Disabled();
			this._soundSource.mute = true;
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			this.UpdateSourceMute();
		}

		private void UpdateSourceMute()
		{
			this._soundSource.mute = !base.IsLocalPlayer && !base.IsSpectated;
		}

		[SerializeField]
		private AudioSource _soundSource;
	}
}

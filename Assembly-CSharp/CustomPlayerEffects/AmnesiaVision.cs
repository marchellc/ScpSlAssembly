using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public class AmnesiaVision : StatusEffectBase, ISoundtrackMutingEffect
	{
		public bool MuteSoundtrack
		{
			get
			{
				return false;
			}
		}

		public float LastActive
		{
			get
			{
				return Time.timeSinceLevelLoad - this._lastTime;
			}
		}

		protected override void Enabled()
		{
			base.Enabled();
			this._lastTime = Time.timeSinceLevelLoad;
		}

		private float _lastTime;
	}
}

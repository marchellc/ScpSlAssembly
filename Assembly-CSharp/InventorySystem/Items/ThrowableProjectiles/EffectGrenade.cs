using System;
using Mirror;
using UnityEngine;
using Utils;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class EffectGrenade : TimeGrenade
	{
		public override void ToggleRenderers(bool state)
		{
			base.ToggleRenderers(state);
			this._resyncAudio = state;
		}

		protected override void Update()
		{
			base.Update();
			double time = NetworkTime.time;
			if (!this._resyncAudio || base.TargetTime < time)
			{
				return;
			}
			this._resyncAudio = false;
			if (this._src == null)
			{
				return;
			}
			float length = this._src.clip.length;
			float num = (float)(base.TargetTime - time);
			if (num > length)
			{
				this._src.PlayDelayed(num - length);
				return;
			}
			this._src.Play();
			this._src.time = length - num;
		}

		public virtual void PlayExplosionEffects(Vector3 position)
		{
			Transform transform = global::UnityEngine.Object.Instantiate<GameObject>(this.Effect).transform;
			transform.position = position;
			transform.localScale = Vector3.one;
			global::UnityEngine.Object.Destroy(transform.gameObject, this._destroyTime);
		}

		public override bool ServerFuseEnd()
		{
			if (!base.ServerFuseEnd())
			{
				return false;
			}
			ExplosionUtils.ServerSpawnEffect(base.transform.position, this.Info.ItemId);
			base.DestroySelf();
			return true;
		}

		public override bool Weaved()
		{
			return true;
		}

		public GameObject Effect;

		[SerializeField]
		private float _destroyTime;

		[SerializeField]
		private AudioSource _src;

		private bool _resyncAudio = true;
	}
}

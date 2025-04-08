using System;
using AudioPooling;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Pickups
{
	public class CollisionDetectionPickup : ItemPickupBase
	{
		public event Action<Collision> OnCollided;

		protected virtual float MinSoundCooldown
		{
			get
			{
				return 0.1f;
			}
		}

		private void OnCollisionEnter(Collision collision)
		{
			this.ProcessCollision(collision);
		}

		public float GetRangeOfCollisionVelocity(float sqrVel)
		{
			AudioSource source = AudioSourcePoolManager.GetFree().Source;
			for (int i = this._soundsOverVelocity.Length - 1; i >= 0; i--)
			{
				if (this._soundsOverVelocity[i].TryPlaySound(sqrVel, source))
				{
					source.Stop();
					return source.maxDistance;
				}
			}
			return 0f;
		}

		protected virtual void ProcessCollision(Collision collision)
		{
			Action<Collision> onCollided = this.OnCollided;
			if (onCollided != null)
			{
				onCollided(collision);
			}
			float sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
			if (NetworkServer.active)
			{
				float num = this.Info.WeightKg * sqrMagnitude / 2f;
				if (num > 15f)
				{
					float num2 = num * 0.4f;
					BreakableWindow breakableWindow;
					if (collision.collider.TryGetComponent<BreakableWindow>(out breakableWindow))
					{
						breakableWindow.Damage(num2, null, Vector3.zero);
					}
				}
			}
			this.MakeCollisionSound(sqrMagnitude);
		}

		protected void MakeCollisionSound(float sqrtVelocity)
		{
		}

		public override bool Weaved()
		{
			return true;
		}

		private const float MinimalJoulesToInduceDamage = 15f;

		private const float DamagePerJoule = 0.4f;

		private const float DefaultSoundCooldown = 0.1f;

		[SerializeField]
		private CollisionDetectionPickup.SoundOverVelocity[] _soundsOverVelocity;

		[Serializable]
		private struct SoundOverVelocity
		{
			public bool TryPlaySound(float vel, AudioSource src)
			{
				if (vel < this._minimalVelocity)
				{
					return false;
				}
				src.PlayOneShot(this._randomClips[global::UnityEngine.Random.Range(0, this._randomClips.Length)]);
				src.maxDistance = this._maxRange;
				src.pitch = global::UnityEngine.Random.Range(1f - this._randomizePitch, 1f / (1f - this._randomizePitch));
				return true;
			}

			[SerializeField]
			private float _minimalVelocity;

			[SerializeField]
			private AudioClip[] _randomClips;

			[Range(0f, 0.5f)]
			[SerializeField]
			private float _randomizePitch;

			[SerializeField]
			private float _maxRange;
		}
	}
}

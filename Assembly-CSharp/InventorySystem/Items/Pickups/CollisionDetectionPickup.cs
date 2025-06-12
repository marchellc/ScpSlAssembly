using System;
using AudioPooling;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Pickups;

public class CollisionDetectionPickup : ItemPickupBase
{
	[Serializable]
	private struct SoundOverVelocity
	{
		[SerializeField]
		private float _minimalVelocity;

		[SerializeField]
		private AudioClip[] _randomClips;

		[Range(0f, 0.5f)]
		[SerializeField]
		private float _randomizePitch;

		[SerializeField]
		private float _maxRange;

		public bool TryPlaySound(float vel, AudioSource src)
		{
			if (vel < this._minimalVelocity)
			{
				return false;
			}
			src.PlayOneShot(this._randomClips[UnityEngine.Random.Range(0, this._randomClips.Length)]);
			src.maxDistance = this._maxRange;
			src.pitch = UnityEngine.Random.Range(1f - this._randomizePitch, 1f / (1f - this._randomizePitch));
			return true;
		}
	}

	private const float MinimalJoulesToInduceDamage = 15f;

	private const float DamagePerJoule = 0.4f;

	private const float DefaultSoundCooldown = 0.1f;

	[SerializeField]
	private SoundOverVelocity[] _soundsOverVelocity;

	protected virtual float MinSoundCooldown => 0.1f;

	public event Action<Collision> OnCollided;

	private void OnCollisionEnter(Collision collision)
	{
		this.ProcessCollision(collision);
	}

	public float GetRangeOfCollisionVelocity(float sqrVel)
	{
		AudioSource source = AudioSourcePoolManager.GetFree().Source;
		for (int num = this._soundsOverVelocity.Length - 1; num >= 0; num--)
		{
			if (this._soundsOverVelocity[num].TryPlaySound(sqrVel, source))
			{
				source.Stop();
				return source.maxDistance;
			}
		}
		return 0f;
	}

	protected virtual void ProcessCollision(Collision collision)
	{
		this.OnCollided?.Invoke(collision);
		float sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
		if (NetworkServer.active)
		{
			float num = base.Info.WeightKg * sqrMagnitude / 2f;
			if (num > 15f)
			{
				float damage = num * 0.4f;
				if (collision.collider.TryGetComponent<BreakableWindow>(out var component))
				{
					component.Damage(damage, null, Vector3.zero);
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
}

using System;
using PlayerRoles.Spectating;
using UnityEngine;

public class ExplosionCameraShake : MonoBehaviour
{
	private void Update()
	{
		this.force -= Time.deltaTime / this.deductSpeed;
		this.force = Mathf.Clamp01(this.force);
	}

	private void Awake()
	{
		ExplosionCameraShake.singleton = this;
		SpectatorTargetTracker.OnTargetChanged += this.StopShake;
	}

	private void OnDestroy()
	{
		SpectatorTargetTracker.OnTargetChanged -= this.StopShake;
	}

	private void StopShake()
	{
		this.force = 0f;
	}

	public void Shake(float explosionForce)
	{
		if (explosionForce > this.force)
		{
			this.force = explosionForce;
		}
	}

	public float force;

	public float deductSpeed = 2f;

	public static ExplosionCameraShake singleton;
}

using PlayerRoles.Spectating;
using UnityEngine;

public class ExplosionCameraShake : MonoBehaviour
{
	public float force;

	public float deductSpeed = 2f;

	public static ExplosionCameraShake singleton;

	private void Update()
	{
		this.force -= Time.deltaTime / this.deductSpeed;
		this.force = Mathf.Clamp01(this.force);
	}

	private void Awake()
	{
		ExplosionCameraShake.singleton = this;
		SpectatorTargetTracker.OnTargetChanged += StopShake;
	}

	private void OnDestroy()
	{
		SpectatorTargetTracker.OnTargetChanged -= StopShake;
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
}

using System;
using System.Collections;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;
using UnityStandardAssets.Effects;

public class ExplosionPhysicsForce : MonoBehaviour
{
	private IEnumerator Start()
	{
		yield return null;
		float num = 0f;
		if (base.GetComponent<ParticleSystemMultiplier>() != null)
		{
			num = base.GetComponent<ParticleSystemMultiplier>().multiplier;
		}
		float num2 = 10f * num;
		Collider[] array = Physics.OverlapSphere(base.transform.position, num2);
		List<Rigidbody> list = ListPool<Rigidbody>.Shared.Rent();
		foreach (Collider collider in array)
		{
			if (collider.attachedRigidbody != null && !list.Contains(collider.attachedRigidbody))
			{
				list.Add(collider.attachedRigidbody);
			}
		}
		foreach (Rigidbody rigidbody in list)
		{
			rigidbody.AddExplosionForce(this.explosionForce * num, base.transform.position, num2, 1f * num, ForceMode.Impulse);
		}
		ListPool<Rigidbody>.Shared.Return(list);
		yield break;
	}

	public float explosionForce = 4f;
}

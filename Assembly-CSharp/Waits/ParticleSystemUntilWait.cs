using UnityEngine;

namespace Waits;

public class ParticleSystemUntilWait : UntilWait
{
	public ParticleSystem particleSystem;

	protected override bool Predicate()
	{
		return !this.particleSystem.IsAlive();
	}
}

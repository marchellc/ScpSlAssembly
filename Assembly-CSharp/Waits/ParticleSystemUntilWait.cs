using System;
using UnityEngine;

namespace Waits
{
	public class ParticleSystemUntilWait : UntilWait
	{
		protected override bool Predicate()
		{
			return !this.particleSystem.IsAlive();
		}

		public ParticleSystem particleSystem;
	}
}

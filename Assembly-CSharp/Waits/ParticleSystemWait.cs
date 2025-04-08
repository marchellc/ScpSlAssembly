using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Waits
{
	public class ParticleSystemWait : Wait
	{
		protected virtual void Awake()
		{
			this.isAliveDelegate = new Func<bool>(this.particleSystem.IsAlive);
		}

		public override IEnumerator<float> _Run()
		{
			yield return Timing.WaitUntilFalse(this.isAliveDelegate);
			yield break;
		}

		private Func<bool> isAliveDelegate;

		public ParticleSystem particleSystem;
	}
}

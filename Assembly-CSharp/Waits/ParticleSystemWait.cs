using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Waits;

public class ParticleSystemWait : Wait
{
	private Func<bool> isAliveDelegate;

	public ParticleSystem particleSystem;

	protected virtual void Awake()
	{
		this.isAliveDelegate = this.particleSystem.IsAlive;
	}

	public override IEnumerator<float> _Run()
	{
		yield return Timing.WaitUntilFalse(this.isAliveDelegate);
	}
}

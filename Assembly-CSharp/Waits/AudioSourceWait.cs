using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Waits
{
	public class AudioSourceWait : Wait
	{
		public override IEnumerator<float> _Run()
		{
			yield return Timing.WaitUntilFalse(() => this.audioSource.isPlaying);
			yield break;
		}

		public AudioSource audioSource;
	}
}

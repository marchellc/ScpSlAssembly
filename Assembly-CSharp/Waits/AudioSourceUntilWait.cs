using System;
using UnityEngine;

namespace Waits
{
	public class AudioSourceUntilWait : UntilWait
	{
		protected override bool Predicate()
		{
			return !this.audioSource.isPlaying;
		}

		public AudioSource audioSource;
	}
}

using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

namespace Waits
{
	public abstract class WaitManager : MonoBehaviour
	{
		protected virtual void Awake()
		{
			this.waits = base.GetComponents<Wait>();
			this.waitHandles = new CoroutineHandle[this.waits.Length];
		}

		protected void StartAll()
		{
			for (int i = 0; i < this.waits.Length; i++)
			{
				this.waitHandles[i] = Timing.RunCoroutine(this.waits[i]._Run());
			}
		}

		public abstract IEnumerator<float> _Run();

		protected Wait[] waits;

		protected CoroutineHandle[] waitHandles;
	}
}

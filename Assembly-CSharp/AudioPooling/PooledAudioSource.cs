using System;
using UnityEngine;

namespace AudioPooling
{
	public class PooledAudioSource : MonoBehaviour
	{
		public bool Locked { get; set; }

		public ulong TotalRecycles { get; private set; }

		public bool Pooled { get; private set; }

		public AudioSource Source
		{
			get
			{
				if (!this._sourceCached)
				{
					this._audioSource = base.GetComponent<AudioSource>();
					this._sourceCached = true;
				}
				return this._audioSource;
			}
		}

		public Transform FastTransform
		{
			get
			{
				if (!this._transformCached)
				{
					this._transform = base.transform;
					this._transformCached = true;
				}
				return this._transform;
			}
		}

		public bool AllowRecycling
		{
			get
			{
				return !this.Locked && !this.Source.isPlaying;
			}
		}

		internal void OnRecycled()
		{
			this.Pooled = false;
			ulong totalRecycles = this.TotalRecycles;
			this.TotalRecycles = totalRecycles + 1UL;
		}

		internal void OnPooled()
		{
			this.Pooled = true;
		}

		private AudioSource _audioSource;

		private bool _sourceCached;

		private Transform _transform;

		private bool _transformCached;
	}
}

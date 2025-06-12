using UnityEngine;

namespace AudioPooling;

public class PooledAudioSource : MonoBehaviour
{
	private AudioSource _audioSource;

	private bool _sourceCached;

	private Transform _transform;

	private bool _transformCached;

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
			if (!this.Locked)
			{
				return !this.Source.isPlaying;
			}
			return false;
		}
	}

	internal void OnRecycled()
	{
		this.Pooled = false;
		this.TotalRecycles++;
	}

	internal void OnPooled()
	{
		this.Pooled = true;
	}
}

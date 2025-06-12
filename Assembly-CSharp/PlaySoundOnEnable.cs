using UnityEngine;

public class PlaySoundOnEnable : MonoBehaviour
{
	private AudioSource audio;

	private void Awake()
	{
		this.audio = base.GetComponent<AudioSource>();
	}

	private void OnEnable()
	{
		if (this.audio != null && !this.audio.isPlaying)
		{
			this.audio.Play();
		}
	}
}

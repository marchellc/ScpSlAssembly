using System;
using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
	private void Update()
	{
		this.curState = Mathf.Lerp(this.curState, this.creditsHolder.activeSelf ? 1f : 0f, this.lerpSpeed * Time.deltaTime);
		this.mainSource.mute = (double)this.curState > 0.5;
		this.creditsSource.volume = this.curState;
		if (this.creditsChanged != this.creditsHolder.activeSelf)
		{
			this.creditsChanged = this.creditsHolder.activeSelf;
			if (this.creditsChanged)
			{
				this.creditsSource.Play();
			}
		}
	}

	private float curState;

	public float lerpSpeed = 1f;

	private bool creditsChanged;

	[Space(15f)]
	public AudioSource mainSource;

	public AudioSource creditsSource;

	[Space(8f)]
	public GameObject creditsHolder;
}

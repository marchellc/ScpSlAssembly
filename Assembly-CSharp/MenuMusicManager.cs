using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
	private float curState;

	public float lerpSpeed = 1f;

	private bool creditsChanged;

	[Space(15f)]
	public AudioSource mainSource;

	public AudioSource creditsSource;

	[Space(8f)]
	public GameObject creditsHolder;

	private void Update()
	{
		curState = Mathf.Lerp(curState, creditsHolder.activeSelf ? 1f : 0f, lerpSpeed * Time.deltaTime);
		mainSource.mute = (double)curState > 0.5;
		creditsSource.volume = curState;
		if (creditsChanged != creditsHolder.activeSelf)
		{
			creditsChanged = creditsHolder.activeSelf;
			if (creditsChanged)
			{
				creditsSource.Play();
			}
		}
	}
}

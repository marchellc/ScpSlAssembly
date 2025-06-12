using UnityEngine;

public class TextMessage : MonoBehaviour
{
	public float spacing = 15.5f;

	public float xOffset = 3f;

	public float lerpSpeed = 3f;

	public float position;

	public float remainingLife;

	private Vector3 GetPosition()
	{
		return new Vector3(this.xOffset, this.spacing * this.position, 0f);
	}

	private void Start()
	{
	}

	private void Update()
	{
		this.remainingLife -= Time.deltaTime;
	}
}

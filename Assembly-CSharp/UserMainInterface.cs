using UnityEngine;

public class UserMainInterface : MonoBehaviour
{
	public static UserMainInterface singleton;

	public float LerpSpeed = 4f;

	private void Awake()
	{
		UserMainInterface.singleton = this;
	}

	private void Start()
	{
	}
}

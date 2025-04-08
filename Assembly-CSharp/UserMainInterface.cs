using System;
using UnityEngine;

public class UserMainInterface : MonoBehaviour
{
	private void Awake()
	{
		UserMainInterface.singleton = this;
	}

	private void Start()
	{
	}

	public static UserMainInterface singleton;

	public float LerpSpeed = 4f;
}

using System;
using UnityEngine;

public class InterfaceColorAdjuster : MonoBehaviour
{
	private void Awake()
	{
		PlayerList.ica = this;
	}
}

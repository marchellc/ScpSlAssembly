using System;
using UnityEngine;
using UnityEngine.UI;

public class AspectScaler : MonoBehaviour
{
	private void Update()
	{
		float num = (float)Screen.width / (float)Screen.height;
		if (num > 1.8f)
		{
			this.Scaler.matchWidthOrHeight = 1f;
		}
		if (num < 1.65f)
		{
			this.Scaler.matchWidthOrHeight = 0f;
		}
	}

	public CanvasScaler Scaler;
}

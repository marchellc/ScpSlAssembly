using System;
using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
	private void FixedUpdate()
	{
		this.i++;
		if (this.i > this.framesToNextRotation)
		{
			this.i = 0;
			base.transform.Rotate(Vector3.forward * -45f, Space.Self);
		}
	}

	private int i;

	public int framesToNextRotation = 10;
}

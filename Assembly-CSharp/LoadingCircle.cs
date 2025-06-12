using UnityEngine;

public class LoadingCircle : MonoBehaviour
{
	private int i;

	public int framesToNextRotation = 10;

	private void FixedUpdate()
	{
		this.i++;
		if (this.i > this.framesToNextRotation)
		{
			this.i = 0;
			base.transform.Rotate(Vector3.forward * -45f, Space.Self);
		}
	}
}

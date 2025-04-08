using System;
using UnityEngine;

public class FakeSoundScope : MonoBehaviour
{
	private void Awake()
	{
		this.line = base.GetComponent<LineRenderer>();
	}

	private void LateUpdate()
	{
		Vector3[] array = new Vector3[this.numOfPos];
		float value = global::UnityEngine.Random.value;
		float num = 0f;
		for (int i = 0; i < this.numOfPos; i++)
		{
			float num2 = (float)i / (float)this.numOfPos;
			float num3 = Mathf.Abs(1f - Mathf.Abs(num2 - 0.5f) * 2f);
			array[i][0] = num2 * 100f;
			array[i][2] = Mathf.Sin((float)(i * 7) * value) * num3 * this.maxH * (Mathf.Sin((float)i) / 3f) * num;
		}
		this.line.SetPositions(array);
	}

	public AnimationCurve highOverVolume;

	public int numOfPos;

	private LineRenderer line;

	public float maxH;
}

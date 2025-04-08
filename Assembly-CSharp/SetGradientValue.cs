using System;
using UnityEngine;
using UnityEngine.UI;

public class SetGradientValue : MonoBehaviour
{
	private void Start()
	{
		Image component = base.GetComponent<Image>();
		component.material = new Material(component.material);
		component.material.SetFloat(this._startId, this._startValue);
	}

	private readonly int _startId = Shader.PropertyToID("_Start");

	public float _startValue = 1f;
}

using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class ControlPanelLights : MonoBehaviour
{
	private void Start()
	{
		Timing.RunCoroutine(this._Animate(), Segment.FixedUpdate);
	}

	private IEnumerator<float> _Animate()
	{
		int i = this.emissions.Length;
		while (this != null)
		{
			if (this.targetMat != null)
			{
				this.targetMat.SetTexture(ControlPanelLights._emissionMap, this.emissions[global::UnityEngine.Random.Range(0, i)]);
			}
			yield return Timing.WaitForSeconds(global::UnityEngine.Random.Range(0.2f, 0.8f));
		}
		yield break;
	}

	public Texture[] emissions;

	public Material targetMat;

	private static readonly int _emissionMap = Shader.PropertyToID("_EmissionMap");
}

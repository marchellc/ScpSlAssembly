using System.Collections.Generic;
using MEC;
using UnityEngine;

public class ControlPanelLights : MonoBehaviour
{
	public Texture[] emissions;

	public Material targetMat;

	private static readonly int _emissionMap = Shader.PropertyToID("_EmissionMap");

	private void Start()
	{
		Timing.RunCoroutine(this._Animate(), Segment.FixedUpdate);
	}

	private IEnumerator<float> _Animate()
	{
		int l = this.emissions.Length;
		while (this != null)
		{
			if (this.targetMat != null)
			{
				this.targetMat.SetTexture(ControlPanelLights._emissionMap, this.emissions[Random.Range(0, l)]);
			}
			yield return Timing.WaitForSeconds(Random.Range(0.2f, 0.8f));
		}
	}
}

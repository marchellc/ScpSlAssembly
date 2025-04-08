using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
	private void Start()
	{
		this._source = base.GetComponent<AudioSource>();
		this.LoadInspectorCurves();
	}

	protected void LoadInspectorCurves()
	{
		foreach (CurvePreset curvePreset in this.Curves)
		{
			if (!this._curves.ContainsKey(curvePreset.Type))
			{
				this._curves.Add(curvePreset.Type, curvePreset);
			}
		}
	}

	public void Play(AudioClip clip, FalloffType falloff = FalloffType.Linear, float maxDistance = -1f)
	{
		CurvePreset curvePreset;
		if (!this._curves.TryGetValue(falloff, out curvePreset))
		{
			return;
		}
		this._source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curvePreset.FalloffCurve);
		if (maxDistance > 0f)
		{
			this._source.maxDistance = maxDistance;
		}
		this._source.clip = clip;
		this._source.Play();
	}

	public void Stop()
	{
		if (this._source.isPlaying)
		{
			this._source.Stop();
		}
		this._source.clip = null;
	}

	protected AudioSource _source;

	public CurvePreset[] Curves;

	private readonly Dictionary<FalloffType, CurvePreset> _curves = new Dictionary<FalloffType, CurvePreset>();
}

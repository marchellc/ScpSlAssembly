using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
	protected AudioSource _source;

	public CurvePreset[] Curves;

	private readonly Dictionary<FalloffType, CurvePreset> _curves = new Dictionary<FalloffType, CurvePreset>();

	private void Start()
	{
		this._source = base.GetComponent<AudioSource>();
		this.LoadInspectorCurves();
	}

	protected void LoadInspectorCurves()
	{
		CurvePreset[] curves = this.Curves;
		foreach (CurvePreset curvePreset in curves)
		{
			if (!this._curves.ContainsKey(curvePreset.Type))
			{
				this._curves.Add(curvePreset.Type, curvePreset);
			}
		}
	}

	public void Play(AudioClip clip, FalloffType falloff = FalloffType.Linear, float maxDistance = -1f)
	{
		if (this._curves.TryGetValue(falloff, out var value))
		{
			this._source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, value.FalloffCurve);
			if (maxDistance > 0f)
			{
				this._source.maxDistance = maxDistance;
			}
			this._source.clip = clip;
			this._source.Play();
		}
	}

	public void Stop()
	{
		if (this._source.isPlaying)
		{
			this._source.Stop();
		}
		this._source.clip = null;
	}
}

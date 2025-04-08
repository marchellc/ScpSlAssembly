using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class StackableGradientSky : MonoBehaviour
{
	private float Weight
	{
		get
		{
			if (!this._volume.enabled)
			{
				return 0f;
			}
			return this._volume.weight;
		}
	}

	public static void GetCombinedColor(out Color top, out Color mid, out Color bottom, out float diffusion)
	{
		top = Color.black;
		mid = Color.black;
		bottom = Color.black;
		diffusion = 1f;
		foreach (StackableGradientSky stackableGradientSky in StackableGradientSky.ActiveInstances)
		{
			float weight = stackableGradientSky.Weight;
			top = Color.Lerp(top, stackableGradientSky._top, weight);
			mid = Color.Lerp(mid, stackableGradientSky._middle, weight);
			bottom = Color.Lerp(bottom, stackableGradientSky._bottom, weight);
			diffusion = Mathf.Lerp(diffusion, stackableGradientSky._gradientDiffusion, weight);
		}
	}

	private void Awake()
	{
		this._volume = base.GetComponent<Volume>();
		for (int i = 0; i < StackableGradientSky.ActiveInstances.Count; i++)
		{
			if (this._volume.priority <= StackableGradientSky.ActiveInstances[i]._volume.priority)
			{
				StackableGradientSky.ActiveInstances.Insert(i, this);
				return;
			}
		}
		StackableGradientSky.ActiveInstances.Add(this);
	}

	private void OnDestroy()
	{
		StackableGradientSky.ActiveInstances.Remove(this);
	}

	[ColorUsage(true, true)]
	[SerializeField]
	private Color _top;

	[ColorUsage(true, true)]
	[SerializeField]
	private Color _middle;

	[ColorUsage(true, true)]
	[SerializeField]
	private Color _bottom;

	[SerializeField]
	private float _gradientDiffusion;

	private Volume _volume;

	private static readonly List<StackableGradientSky> ActiveInstances = new List<StackableGradientSky>();
}

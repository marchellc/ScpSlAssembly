using System;
using UnityEngine;

public class CachedLayerMask
{
	public LayerMask Mask
	{
		get
		{
			if (this._cachedMask == 0)
			{
				this._cachedMask = LayerMask.GetMask(this._layers);
			}
			return this._cachedMask;
		}
	}

	public CachedLayerMask(params string[] layers)
	{
		this._layers = layers;
	}

	public static implicit operator int(CachedLayerMask mask)
	{
		return mask.Mask;
	}

	private int _cachedMask;

	private readonly string[] _layers;
}

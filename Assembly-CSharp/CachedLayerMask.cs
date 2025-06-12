using UnityEngine;

public class CachedLayerMask
{
	private int _cachedMask;

	private readonly string[] _layers;

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
}

using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

[Serializable]
public class AnimatorLayerMask
{
	[SerializeField]
	private int _maskValue;

	private int _cachedMaskValue;

	private int[] _cachedMaskArray;

	public int[] Layers
	{
		get
		{
			if (this._cachedMaskValue == this._maskValue && this._cachedMaskArray != null)
			{
				return this._cachedMaskArray;
			}
			uint num = (uint)this._maskValue;
			int num2 = 0;
			while (num != 0)
			{
				num2++;
				num &= num - 1;
			}
			if (this._cachedMaskArray == null || this._cachedMaskArray.Length != num2)
			{
				this._cachedMaskArray = new int[num2];
			}
			for (int i = 0; i < 32; i++)
			{
				int num3 = 1 << i;
				if (num3 > this._maskValue)
				{
					break;
				}
				if ((this._maskValue & num3) != 0)
				{
					this._cachedMaskArray[--num2] = i;
				}
			}
			this._cachedMaskValue = this._maskValue;
			return this._cachedMaskArray;
		}
		set
		{
			this._maskValue = 0;
			value.ForEach(delegate(int x)
			{
				this.SetLayer(x, state: true);
			});
		}
	}

	public void SetLayer(int layerIndex, bool state)
	{
		int num = 1 << layerIndex;
		if (state)
		{
			this._maskValue |= num;
		}
		else
		{
			this._maskValue &= ~num;
		}
	}

	public bool GetLayer(int layerIndex)
	{
		int num = 1 << layerIndex;
		return (this._maskValue & num) == num;
	}

	public void SetWeight(Animator anim, float weight)
	{
		this.SetWeight(anim.SetLayerWeight, weight);
	}

	public void SetWeight(Action<int, float> setter, float weight)
	{
		int[] layers = this.Layers;
		foreach (int arg in layers)
		{
			setter(arg, weight);
		}
	}
}

using System;
using UnityEngine;

namespace FacilitySoundtrack
{
	public class SoundtrackManager : MonoBehaviour
	{
		private void Update()
		{
			int num = this._layers.Length - 1;
			float num2 = 0f;
			for (int i = num; i >= 0; i--)
			{
				float num3 = Mathf.Clamp01(this._layers[i].Weight);
				float num4 = Mathf.Max(1f - num2, 0f) * num3;
				if (!this._layers[i].Additive)
				{
					num2 += num3;
				}
				this._layers[i].UpdateVolume(num4);
			}
		}

		[SerializeField]
		private SoundtrackLayerBase[] _layers;
	}
}

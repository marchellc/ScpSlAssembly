using System;
using UnityEngine;

namespace UserSettings.VideoSettings
{
	public struct AspectRatio
	{
		public bool CheckRes(Resolution res)
		{
			float num = (float)res.width / (float)res.height;
			return num >= this.RatioMinMax.x && num <= this.RatioMinMax.y;
		}

		public float Horizontal;

		public float Vertical;

		public Vector2 RatioMinMax;
	}
}

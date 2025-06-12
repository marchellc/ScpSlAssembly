using UnityEngine;

namespace UserSettings.VideoSettings;

public struct AspectRatio
{
	public float Horizontal;

	public float Vertical;

	public Vector2 RatioMinMax;

	public bool CheckRes(Resolution res)
	{
		float num = (float)res.width / (float)res.height;
		if (num >= this.RatioMinMax.x)
		{
			return num <= this.RatioMinMax.y;
		}
		return false;
	}
}

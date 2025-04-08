using System;

public static class EaseInUtils
{
	public static float EaseInOutCubic(float t)
	{
		if (t < 0.5f)
		{
			float num = t * t * t;
			return 4f * num;
		}
		float num2 = -2f * t + 2f;
		float num3 = num2 * num2 * num2;
		return 1f - num3 / 2f;
	}
}

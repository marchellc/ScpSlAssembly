using System;
using UnityEngine;

public static class AudioUtils
{
	public static void SetSpace(this AudioSource src, bool is3D)
	{
		if (is3D)
		{
			src.Set3D();
			return;
		}
		src.Set2D();
	}

	public static void Set3D(this AudioSource src)
	{
		src.spatialBlend = 1f;
		src.spread = 0f;
	}

	public static void Set2D(this AudioSource src)
	{
		src.spatialBlend = 0f;
		src.spread = 360f;
	}
}

using System;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class TextureAnimator : MonoBehaviour
{
	private void Start()
	{
		Timing.RunCoroutine(this._Animate(), Segment.FixedUpdate);
	}

	private IEnumerator<float> _Animate()
	{
		while (this != null)
		{
			int num;
			for (int i = 0; i < this.textures.Length; i = num + 1)
			{
				this.optionalLight.enabled = i < this.lightRange;
				this.targetRenderer.material = this.textures[i];
				int x = 0;
				while ((float)x < 50f * this.cooldown)
				{
					yield return 0f;
					num = x;
					x = num + 1;
				}
				num = i;
			}
		}
		yield break;
	}

	public Material[] textures;

	public Renderer targetRenderer;

	public float cooldown;

	public Light optionalLight;

	public int lightRange;
}

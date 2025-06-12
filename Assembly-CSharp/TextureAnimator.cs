using System.Collections.Generic;
using MEC;
using UnityEngine;

public class TextureAnimator : MonoBehaviour
{
	public Material[] textures;

	public Renderer targetRenderer;

	public float cooldown;

	public Light optionalLight;

	public int lightRange;

	private void Start()
	{
		Timing.RunCoroutine(this._Animate(), Segment.FixedUpdate);
	}

	private IEnumerator<float> _Animate()
	{
		while (this != null)
		{
			for (int i = 0; i < this.textures.Length; i++)
			{
				this.optionalLight.enabled = i < this.lightRange;
				this.targetRenderer.material = this.textures[i];
				for (int x = 0; (float)x < 50f * this.cooldown; x++)
				{
					yield return 0f;
				}
			}
		}
	}
}

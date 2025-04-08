using System;
using UnityEngine;

public class TextureMaterialLanguage : MonoBehaviour
{
	private void Start()
	{
		this.mat.mainTexture = this.englishVersion;
	}

	public Texture englishVersion;

	public Material mat;
}

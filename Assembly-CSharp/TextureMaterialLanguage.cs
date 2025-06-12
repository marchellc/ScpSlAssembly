using UnityEngine;

public class TextureMaterialLanguage : MonoBehaviour
{
	public Texture englishVersion;

	public Material mat;

	private void Start()
	{
		this.mat.mainTexture = this.englishVersion;
	}
}

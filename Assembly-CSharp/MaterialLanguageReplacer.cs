using UnityEngine;

public class MaterialLanguageReplacer : MonoBehaviour
{
	public Material englishVersion;

	private void Start()
	{
		base.GetComponent<Renderer>().material = this.englishVersion;
	}

	private void OnDestroy()
	{
		Object.Destroy(base.GetComponent<Renderer>().material);
	}
}

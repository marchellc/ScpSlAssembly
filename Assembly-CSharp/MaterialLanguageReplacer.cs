using System;
using UnityEngine;

public class MaterialLanguageReplacer : MonoBehaviour
{
	private void Start()
	{
		base.GetComponent<Renderer>().material = this.englishVersion;
	}

	private void OnDestroy()
	{
		global::UnityEngine.Object.Destroy(base.GetComponent<Renderer>().material);
	}

	public Material englishVersion;
}

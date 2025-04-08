using System;
using UnityEngine;

namespace CustomRendering
{
	public class PostProcessingVolumes : MonoBehaviour
	{
		private void Awake()
		{
			global::UnityEngine.Object.Destroy(base.gameObject);
		}
	}
}

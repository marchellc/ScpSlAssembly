using UnityEngine;

namespace CustomRendering;

public class PostProcessingVolumes : MonoBehaviour
{
	private void Awake()
	{
		Object.Destroy(base.gameObject);
	}
}

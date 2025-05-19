using UnityEngine;

public class Skybox : MonoBehaviour
{
	[SerializeField]
	private Bounds _bounds;

	[SerializeField]
	private Transform _cubemapTr;

	private Renderer _rend;

	private void Awake()
	{
		_rend = _cubemapTr.GetComponent<Renderer>();
		MainCameraController.OnUpdated += OnCamUpdated;
	}

	private void OnDestroy()
	{
		MainCameraController.OnUpdated -= OnCamUpdated;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(_bounds.center, _bounds.size);
	}

	private void OnCamUpdated()
	{
		Vector3 position = MainCameraController.CurrentCamera.position;
		bool flag = _bounds.Contains(position);
		_rend.enabled = flag;
		if (flag)
		{
			_cubemapTr.position = position;
		}
	}
}

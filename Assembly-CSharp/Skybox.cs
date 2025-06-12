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
		this._rend = this._cubemapTr.GetComponent<Renderer>();
		MainCameraController.OnUpdated += OnCamUpdated;
	}

	private void OnDestroy()
	{
		MainCameraController.OnUpdated -= OnCamUpdated;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireCube(this._bounds.center, this._bounds.size);
	}

	private void OnCamUpdated()
	{
		Vector3 position = MainCameraController.CurrentCamera.position;
		bool flag = this._bounds.Contains(position);
		this._rend.enabled = flag;
		if (flag)
		{
			this._cubemapTr.position = position;
		}
	}
}

using System;
using UnityEngine;

public class Skybox : MonoBehaviour
{
	private void Awake()
	{
		this._rend = this._cubemapTr.GetComponent<Renderer>();
		MainCameraController.OnUpdated += this.OnCamUpdated;
	}

	private void OnDestroy()
	{
		MainCameraController.OnUpdated -= this.OnCamUpdated;
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
		if (!flag)
		{
			return;
		}
		this._cubemapTr.position = position;
	}

	[SerializeField]
	private Bounds _bounds;

	[SerializeField]
	private Transform _cubemapTr;

	private Renderer _rend;
}

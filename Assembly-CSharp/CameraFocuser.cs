using System;
using UnityEngine;

public class CameraFocuser : MonoBehaviour
{
	private void OnTriggerStay(Collider other)
	{
		ReferenceHub hub = ReferenceHub.GetHub(other.transform.root.gameObject);
		if (hub != null && hub.characterClassManager.isLocalPlayer)
		{
			base.transform.LookAt(this.lookTarget);
			Mathf.Clamp(Quaternion.Angle(hub.PlayerCameraReference.rotation, base.transform.rotation), this.minimumAngle, 70f);
		}
	}

	public Transform lookTarget;

	public float targetFovScale = 1f;

	public float minimumAngle;
}

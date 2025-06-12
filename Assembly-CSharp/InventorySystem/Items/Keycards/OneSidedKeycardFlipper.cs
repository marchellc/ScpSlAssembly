using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class OneSidedKeycardFlipper : MonoBehaviour
{
	private Transform _tr;

	[SerializeField]
	private Vector3 _facingDirection;

	[SerializeField]
	private Vector3 _flipRotation;

	private void Awake()
	{
		this._tr = base.transform;
	}

	private void LateUpdate()
	{
		this._tr.GetPositionAndRotation(out var position, out var rotation);
		Vector3 lhs = rotation * this._facingDirection;
		Vector3 rhs = MainCameraController.LastPosition - position;
		if (!(Vector3.Dot(lhs, rhs) >= 0f))
		{
			this._tr.localEulerAngles += this._flipRotation;
		}
	}
}

using UnityEngine;

namespace InventorySystem.Items.SwayControllers;

public class FixedSway : IItemSwayController
{
	private readonly Vector3 _fixedPosition;

	private readonly Quaternion _fixedRotation;

	private Transform _transform;

	private bool _update;

	public FixedSway(Transform targetTransform, Vector3 fixedPosition, Vector3 fixedRotation)
	{
		this._transform = targetTransform;
		this._fixedPosition = fixedPosition;
		this._fixedRotation = Quaternion.Euler(fixedRotation);
		this._update = true;
	}

	public void SetTransform(Transform targetTransform)
	{
		this._transform = targetTransform;
		this._update = true;
	}

	public void UpdateSway()
	{
		if (this._update && !(this._transform == null))
		{
			this._transform.localPosition = this._fixedPosition;
			this._transform.localRotation = this._fixedRotation;
			this._update = false;
		}
	}
}

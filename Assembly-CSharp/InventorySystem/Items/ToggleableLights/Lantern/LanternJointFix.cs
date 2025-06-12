using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern;

public class LanternJointFix : MonoBehaviour
{
	private Quaternion _initialLocalRotation;

	private Vector3 _initialLocalPosition;

	private Quaternion _localRotationOnDisable;

	private Vector3 _localPositionOnDisable;

	private bool _hasDisabled;

	private Transform _transform;

	private void Awake()
	{
		this._transform = base.transform;
		this._initialLocalRotation = this._transform.localRotation;
		this._initialLocalPosition = this._transform.localPosition;
	}

	private void OnDisable()
	{
		this._localRotationOnDisable = this._transform.localRotation;
		this._transform.localRotation = this._initialLocalRotation;
		this._localPositionOnDisable = this._transform.localPosition;
		this._transform.localPosition = this._initialLocalPosition;
		this._hasDisabled = true;
	}

	private void Update()
	{
		if (this._hasDisabled)
		{
			this._hasDisabled = false;
			this._transform.localRotation = this._localRotationOnDisable;
			this._transform.localPosition = this._localPositionOnDisable;
		}
	}
}

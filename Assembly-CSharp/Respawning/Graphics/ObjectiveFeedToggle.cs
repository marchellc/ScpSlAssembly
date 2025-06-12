using UnityEngine;
using UserSettings.GUIElements;

namespace Respawning.Graphics;

public class ObjectiveFeedToggle : UserSettingsToggle
{
	[SerializeField]
	private RectTransform _targetTransform;

	[SerializeField]
	private float _disabledRotation = 180f;

	[SerializeField]
	private float _rotationSpeed = 7.5f;

	private float _targetRotation;

	private float _cachedRotation;

	protected override void SetValueAndTriggerEvent(bool val)
	{
		base.SetValueAndTriggerEvent(val);
		this.SetRotationStatus(val);
	}

	protected override void SetValueWithoutNotify(bool val)
	{
		base.SetValueWithoutNotify(val);
		this.SetRotationStatus(val);
	}

	protected override void Awake()
	{
		base.Awake();
		this._cachedRotation = this._targetTransform.eulerAngles.z;
		this.SetRotationStatus(base.StoredValue, instantRotation: true);
	}

	private void SetRotationStatus(bool isEnabled, bool instantRotation = false)
	{
		this._targetRotation = (isEnabled ? this._cachedRotation : this._disabledRotation);
		if (instantRotation)
		{
			Vector3 eulerAngles = this._targetTransform.eulerAngles;
			this._targetTransform.rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, this._targetRotation);
		}
	}

	private void Update()
	{
		Vector3 eulerAngles = this._targetTransform.eulerAngles;
		if (eulerAngles.z != this._targetRotation)
		{
			float z = Mathf.LerpAngle(eulerAngles.z, this._targetRotation, this._rotationSpeed * Time.deltaTime);
			this._targetTransform.rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, z);
		}
	}
}

using System;
using UnityEngine;
using UserSettings.GUIElements;

namespace Respawning.Graphics
{
	public class ObjectiveFeedToggle : UserSettingsToggle
	{
		protected override void SetValueAndTriggerEvent(bool val)
		{
			base.SetValueAndTriggerEvent(val);
			this.SetRotationStatus(val, false);
		}

		protected override void SetValueWithoutNotify(bool val)
		{
			base.SetValueWithoutNotify(val);
			this.SetRotationStatus(val, false);
		}

		protected override void Awake()
		{
			base.Awake();
			this._cachedRotation = this._targetTransform.eulerAngles.z;
			this.SetRotationStatus(base.StoredValue, true);
		}

		private void SetRotationStatus(bool isEnabled, bool instantRotation = false)
		{
			this._targetRotation = (isEnabled ? this._cachedRotation : this._disabledRotation);
			if (!instantRotation)
			{
				return;
			}
			Vector3 eulerAngles = this._targetTransform.eulerAngles;
			this._targetTransform.rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, this._targetRotation);
		}

		private void Update()
		{
			Vector3 eulerAngles = this._targetTransform.eulerAngles;
			if (eulerAngles.z == this._targetRotation)
			{
				return;
			}
			float num = Mathf.LerpAngle(eulerAngles.z, this._targetRotation, this._rotationSpeed * Time.deltaTime);
			this._targetTransform.rotation = Quaternion.Euler(eulerAngles.x, eulerAngles.y, num);
		}

		[SerializeField]
		private RectTransform _targetTransform;

		[SerializeField]
		private float _disabledRotation = 180f;

		[SerializeField]
		private float _rotationSpeed = 7.5f;

		private float _targetRotation;

		private float _cachedRotation;
	}
}

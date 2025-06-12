using System;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public class OffsetableSimpleWearable : SimpleWearable
{
	[Serializable]
	private class OffsetConditionPair
	{
		[SerializeField]
		private GameObject _condition;

		[SerializeField]
		private bool _inverseCondition;

		[SerializeField]
		private float _upOffset;

		[SerializeField]
		private float _forwardOffset;

		private bool? _lastActive;

		public void Refresh(OffsetableSimpleWearable wearable)
		{
			bool activeSelf = this._condition.activeSelf;
			if (this._lastActive != activeSelf)
			{
				Transform sourceTr = wearable.TargetObject.SourceTr;
				Vector3 originalLocalPosition = wearable._originalLocalPosition;
				sourceTr.localPosition = originalLocalPosition;
				if (this._inverseCondition ? (!activeSelf) : activeSelf)
				{
					Quaternion rotation = wearable._parent.rotation;
					Vector3 vector = rotation * Vector3.up * this._upOffset;
					Vector3 vector2 = rotation * Vector3.forward * this._forwardOffset;
					sourceTr.position += vector + vector2;
				}
				this._lastActive = activeSelf;
			}
		}
	}

	private Vector3 _originalLocalPosition;

	private Transform _parent;

	[SerializeField]
	private OffsetConditionPair[] _offsets;

	public override void Initialize(WearableSubcontroller model)
	{
		base.Initialize(model);
		Transform sourceTr = base.TargetObject.SourceTr;
		this._originalLocalPosition = sourceTr.localPosition;
		this._parent = sourceTr.parent;
	}

	private void LateUpdate()
	{
		if (base.IsVisible)
		{
			for (int i = 0; i < this._offsets.Length; i++)
			{
				this._offsets[i].Refresh(this);
			}
		}
	}
}

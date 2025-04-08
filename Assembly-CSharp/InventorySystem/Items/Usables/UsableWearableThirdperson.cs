using System;
using InventorySystem.Items.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Usables
{
	public class UsableWearableThirdperson : UsableItemThirdperson
	{
		protected override void OnUsingStatusChanged()
		{
			base.OnUsingStatusChanged();
			this._elapsed = 0f;
			this.SetGfxVisibility(true);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._elapsed = 0f;
			this._lastWeight = 0f;
			this.UpdateOverrides(0f);
			this.SetGfxVisibility(true);
		}

		public override LookatData ProcessLookat(LookatData data)
		{
			LookatData lookatData = base.ProcessLookat(data);
			return data.LerpTo(lookatData, this._ikWeightOverTime.Evaluate(this._elapsed));
		}

		public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			ThirdpersonLayerWeight weightForLayer = base.GetWeightForLayer(layer);
			return new ThirdpersonLayerWeight(weightForLayer.Weight * this._ikWeightOverTime.Evaluate(this._elapsed), weightForLayer.AllowOther);
		}

		protected override void Update()
		{
			base.Update();
			if (base.IsUsing)
			{
				this._elapsed += Time.deltaTime;
				this._lastWeight = this._positionOverrideWeightOverTime.Evaluate(this._elapsed);
			}
			else
			{
				float num = Time.deltaTime / 0.2f;
				this._lastWeight = Mathf.Clamp01(this._lastWeight - num);
			}
			this.UpdateOverrides(this._lastWeight);
		}

		protected override void Awake()
		{
			base.Awake();
			this._gfxToDisable = this._movable.gameObject;
			this._lScale = this._movable.localScale;
			this._movable.GetLocalPositionAndRotation(out this._lPos, out this._lRot);
		}

		private void UpdateOverrides(float weight)
		{
			if (this._locationModified)
			{
				this._movable.localScale = this._lScale;
				this._movable.SetLocalPositionAndRotation(this._lPos, this._lRot);
				this._locationModified = false;
			}
			WearableSubcontroller wearableSubcontroller;
			if (!base.TargetModel.TryGetSubcontroller<WearableSubcontroller>(out wearableSubcontroller))
			{
				return;
			}
			WearableSubcontroller.DisplayableWearable displayableWearable;
			if (!wearableSubcontroller.TryGetWearable(this._targetSlot, out displayableWearable))
			{
				return;
			}
			if (displayableWearable.TargetObject.activeSelf)
			{
				this.SetGfxVisibility(false);
			}
			if (weight <= 0f)
			{
				return;
			}
			Vector3 vector;
			Quaternion quaternion;
			this._movable.GetPositionAndRotation(out vector, out quaternion);
			Transform targetTransform = displayableWearable.TargetTransform;
			vector = Vector3.Lerp(vector, targetTransform.TransformPoint(this._positionOffset), weight);
			quaternion = Quaternion.Lerp(quaternion, Quaternion.Euler(this._rotationOffset) * targetTransform.rotation, weight);
			this._movable.SetPositionAndRotation(vector, quaternion);
			this._movable.localScale = Vector3.Lerp(this._lScale, Vector3.Scale(displayableWearable.GlobalScale, this._scaleMultiplier), weight);
			this._locationModified = true;
		}

		private void SetGfxVisibility(bool isVisible)
		{
			bool flag = !isVisible;
			if (flag == this._gfxDisabled)
			{
				return;
			}
			this._gfxDisabled = flag;
			this._gfxToDisable.SetActive(isVisible);
		}

		[SerializeField]
		private AnimationCurve _positionOverrideWeightOverTime;

		[SerializeField]
		private AnimationCurve _ikWeightOverTime;

		[SerializeField]
		private WearableElements _targetSlot;

		[SerializeField]
		private Vector3 _positionOffset;

		[SerializeField]
		private Vector3 _rotationOffset;

		[SerializeField]
		private Vector3 _scaleMultiplier = Vector3.one;

		[SerializeField]
		private Transform _movable;

		private Vector3 _lPos;

		private Quaternion _lRot;

		private Vector3 _lScale;

		private bool _locationModified;

		private bool _gfxDisabled;

		private float _lastWeight;

		private float _elapsed;

		private GameObject _gfxToDisable;

		private const float RewindTimeSeconds = 0.2f;
	}
}

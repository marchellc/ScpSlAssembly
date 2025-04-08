using System;
using UnityEngine;

namespace InventorySystem.Items.SwayControllers
{
	public class FixedSway : IItemSwayController
	{
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
			if (!this._update)
			{
				return;
			}
			if (this._transform == null)
			{
				return;
			}
			this._transform.localPosition = this._fixedPosition;
			this._transform.localRotation = this._fixedRotation;
			this._update = false;
		}

		private readonly Vector3 _fixedPosition;

		private readonly Quaternion _fixedRotation;

		private Transform _transform;

		private bool _update;
	}
}

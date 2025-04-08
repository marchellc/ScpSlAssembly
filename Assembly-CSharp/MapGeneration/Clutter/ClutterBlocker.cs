using System;
using UnityEngine;

namespace MapGeneration.Clutter
{
	public class ClutterBlocker : MonoBehaviour, IClutterBlocker
	{
		public Bounds BlockingBounds
		{
			get
			{
				Vector3 vector;
				Quaternion quaternion;
				this._targetTransform.GetPositionAndRotation(out vector, out quaternion);
				Vector3 vector2 = quaternion * this._blockingBounds.center;
				Vector3 vector3 = (quaternion * this._blockingBounds.size).Abs();
				return new Bounds(vector + vector2, vector3);
			}
		}

		private void Awake()
		{
			IClutterBlocker.Instances.Add(this);
		}

		private void OnDestroy()
		{
			IClutterBlocker.Instances.Remove(this);
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireCube(this.BlockingBounds.center, this.BlockingBounds.size);
		}

		private void OnValidate()
		{
			if (this._targetTransform != null)
			{
				return;
			}
			this._targetTransform = base.transform;
		}

		[SerializeField]
		private Bounds _blockingBounds;

		[SerializeField]
		private Transform _targetTransform;
	}
}

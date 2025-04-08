using System;
using UnityEngine;

namespace ProgressiveCulling
{
	public class CullableRig : DynamicCullableBase
	{
		public event Action OnVisibleAgain;

		protected override Vector3 BoundsOrigin
		{
			get
			{
				if (!(this._rootBone == null))
				{
					return this._rootBone.position;
				}
				return base.transform.position;
			}
		}

		protected override float BoundsSize
		{
			get
			{
				return this._boundsSize;
			}
		}

		protected override void OnVisibilityChanged(bool isVisible)
		{
			foreach (GameObject gameObject in this._targetRenderers)
			{
				if (!(gameObject == null))
				{
					gameObject.SetActive(isVisible);
				}
			}
			if (isVisible)
			{
				Action onVisibleAgain = this.OnVisibleAgain;
				if (onVisibleAgain == null)
				{
					return;
				}
				onVisibleAgain();
			}
		}

		public void SetTargetRenderers(GameObject[] newTargetRenderers)
		{
			this._targetRenderers = newTargetRenderers;
		}

		[SerializeField]
		private float _boundsSize = 1f;

		[SerializeField]
		private Transform _rootBone;

		[SerializeField]
		private GameObject[] _targetRenderers;
	}
}

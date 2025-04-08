using System;
using Mirror;
using UnityEngine;

namespace ProgressiveCulling
{
	public class CullableSimpleDynamic : DynamicCullableBase
	{
		public event Action OnCullChanged;

		protected override float BoundsSize
		{
			get
			{
				return this._boundsSize;
			}
		}

		protected override Vector3 BoundsOrigin
		{
			get
			{
				return base.BoundsOrigin + Vector3.up * this._heightOffset;
			}
		}

		[ContextMenu("Find cullable children")]
		private void FindCullableChildren()
		{
			this._culler.Generate(base.gameObject, null, new Predicate<GameObject>(this.AllowDeactivationFilter), false);
		}

		private bool AllowDeactivationFilter(GameObject go)
		{
			if (go == base.gameObject || !go.activeSelf)
			{
				return false;
			}
			foreach (Component component in go.GetComponentsInChildren<Component>())
			{
				if (component is AudioSource || component is Collider || component is Animator)
				{
					return false;
				}
			}
			return true;
		}

		private void Reset()
		{
			this.FindCullableChildren();
		}

		protected override void OnVisibilityChanged(bool isVisible)
		{
			bool active = NetworkServer.active;
		}

		[SerializeField]
		private float _boundsSize = 1f;

		[SerializeField]
		private float _heightOffset;

		[SerializeField]
		private AutoCuller _culler;
	}
}

using System;
using UnityEngine;

namespace ProgressiveCulling
{
	public abstract class DynamicCullableBase : CullableBehaviour, IRootCullable, ICullable, IBoundsCullable
	{
		protected abstract float BoundsSize { get; }

		protected virtual Vector3 BoundsOrigin
		{
			get
			{
				if (!(this._tr == null))
				{
					return this._tr.position;
				}
				return base.transform.position;
			}
		}

		public override bool ShouldBeVisible
		{
			get
			{
				return CullingCamera.CheckBoundsVisibility(this.WorldspaceBounds);
			}
		}

		public RootCullablePriority Priority
		{
			get
			{
				return RootCullablePriority.Dynamic;
			}
		}

		public Bounds WorldspaceBounds
		{
			get
			{
				return new Bounds(this.BoundsOrigin, Vector3.one * this.BoundsSize);
			}
		}

		protected virtual void Awake()
		{
			this._tr = base.transform;
			CullingCamera.RegisterRootCullable(this);
		}

		protected virtual void OnDestroy()
		{
			CullingCamera.UnregisterRootCullable(this);
		}

		protected override void OnDrawGizmosSelected()
		{
			this._tr = base.transform;
			base.OnDrawGizmosSelected();
		}

		public virtual void SetupCache()
		{
		}

		private Transform _tr;
	}
}

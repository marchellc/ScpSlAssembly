using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressiveCulling
{
	public class CullableArea : CullableBehaviour, IAutoCullerOverrideComponent, IBoundsCullable, ICullable
	{
		public Bounds[] ActivationBounds { get; private set; }

		public Bounds PropsBounds { get; private set; }

		public bool AllowAutoCulling { get; private set; }

		public Bounds WorldspaceBounds
		{
			get
			{
				return this._worldspacePropsBounds;
			}
		}

		public override bool ShouldBeVisible
		{
			get
			{
				if (!CullingCamera.CheckBoundsVisibility(this._worldspacePropsBounds))
				{
					return false;
				}
				if (this._worldspaceActivationBounds == null)
				{
					return true;
				}
				Vector3 lastPosition = MainCameraController.LastPosition;
				foreach (Bounds bounds in this._worldspaceActivationBounds)
				{
					if (bounds.Contains(lastPosition))
					{
						return true;
					}
				}
				return false;
			}
		}

		[ContextMenu("Auto-setup bounds")]
		private void AutoSetupBounds()
		{
			Bounds bounds = new Bounds(base.transform.position, Vector3.zero);
			foreach (Renderer renderer in base.GetComponentsInChildren<Renderer>())
			{
				bounds.Encapsulate(renderer.bounds);
			}
			this.PropsBounds = new Bounds(base.transform.InverseTransformPoint(bounds.center), (Quaternion.Inverse(base.transform.rotation) * bounds.size).Abs());
		}

		private Bounds TransformBounds(Bounds bounds)
		{
			Transform transform = base.transform;
			Vector3 vector = transform.TransformPoint(bounds.center);
			Vector3 vector2 = (transform.rotation * bounds.size).Abs();
			return new Bounds(vector, vector2);
		}

		private void Start()
		{
			CullableArea cullableArea;
			if (base.transform.parent.TryGetComponentInParent(out cullableArea))
			{
				cullableArea._childAreas.Add(this);
			}
			else
			{
				CullableRoom componentInParent = base.GetComponentInParent<CullableRoom>();
				this._worldspacePropsBounds = new Bounds(componentInParent.transform.position, Vector3.one);
				componentInParent.AddChildCullable(this);
			}
			this._worldspacePropsBounds = this.TransformBounds(this.PropsBounds);
			if (this.ActivationBounds == null || this.ActivationBounds.Length == 0)
			{
				this._worldspaceActivationBounds = null;
			}
			else
			{
				int num = this.ActivationBounds.Length;
				this._worldspaceActivationBounds = new Bounds[num];
				for (int i = 0; i < num; i++)
				{
					this._worldspaceActivationBounds[i] = this.TransformBounds(this.ActivationBounds[i]);
				}
			}
			this.GenerateAutoCuller();
			this._autoCuller.SetVisibility(this.ShouldBeVisible);
		}

		private void GenerateAutoCuller()
		{
			this.AllowAutoCulling = true;
			this._autoCuller.Generate(base.gameObject, delegate(GameObject x)
			{
				CullableArea cullableArea;
				return !x.TryGetComponent<CullableArea>(out cullableArea) || cullableArea == this;
			}, new Predicate<GameObject>(CullingMath.GetSafeForDeactivation), false);
			this.AllowAutoCulling = false;
		}

		protected override void OnDrawGizmosSelected()
		{
			if (base.EditorShowGizmos)
			{
				this._worldspacePropsBounds = this.TransformBounds(this.PropsBounds);
			}
			base.OnDrawGizmosSelected();
			if (!base.EditorShowGizmos || this.ActivationBounds == null)
			{
				return;
			}
			Gizmos.color = Color.blue;
			foreach (Bounds bounds in this.ActivationBounds)
			{
				Bounds bounds2 = this.TransformBounds(bounds);
				Gizmos.DrawWireCube(bounds2.center, bounds2.size);
			}
		}

		protected override void OnVisibilityChanged(bool isVisible)
		{
			this._autoCuller.SetVisibility(isVisible);
			foreach (CullableArea cullableArea in this._childAreas)
			{
				cullableArea.SetVisibility(isVisible && cullableArea.ShouldBeVisible);
			}
		}

		protected override void UpdateVisible()
		{
			base.UpdateVisible();
			foreach (CullableArea cullableArea in this._childAreas)
			{
				cullableArea.SetVisibility(cullableArea.ShouldBeVisible);
			}
		}

		private readonly AutoCuller _autoCuller = new AutoCuller();

		private readonly List<CullableArea> _childAreas = new List<CullableArea>();

		private Bounds[] _worldspaceActivationBounds;

		private Bounds _worldspacePropsBounds;
	}
}

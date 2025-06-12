using System.Collections.Generic;
using UnityEngine;

namespace ProgressiveCulling;

public class CullableArea : CullableBehaviour, IAutoCullerOverrideComponent, IBoundsCullable, ICullable
{
	private readonly AutoCuller _autoCuller = new AutoCuller();

	private readonly List<CullableArea> _childAreas = new List<CullableArea>();

	private Bounds[] _worldspaceActivationBounds;

	private Bounds _worldspacePropsBounds;

	[field: SerializeField]
	public Bounds[] ActivationBounds { get; private set; }

	[field: SerializeField]
	public Bounds PropsBounds { get; private set; }

	public bool AllowAutoCulling { get; private set; }

	public Bounds WorldspaceBounds => this._worldspacePropsBounds;

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
			Vector3 lastCamPosition = CullingCamera.LastCamPosition;
			Bounds[] worldspaceActivationBounds = this._worldspaceActivationBounds;
			foreach (Bounds bounds in worldspaceActivationBounds)
			{
				if (bounds.Contains(lastCamPosition))
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
		Renderer[] componentsInChildren = base.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			bounds.Encapsulate(renderer.bounds);
		}
		this.PropsBounds = new Bounds(base.transform.InverseTransformPoint(bounds.center), (Quaternion.Inverse(base.transform.rotation) * bounds.size).Abs());
	}

	private Bounds TransformBounds(Bounds bounds)
	{
		Transform obj = base.transform;
		Vector3 center = obj.TransformPoint(bounds.center);
		Vector3 size = (obj.rotation * bounds.size).Abs();
		return new Bounds(center, size);
	}

	private void Start()
	{
		if (base.transform.parent.TryGetComponentInParent<CullableArea>(out var comp))
		{
			comp._childAreas.Add(this);
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
		this._autoCuller.Generate(base.gameObject, (GameObject x) => !x.TryGetComponent<CullableArea>(out var component) || component == this, CullingMath.GetSafeForDeactivation);
		this.AllowAutoCulling = false;
	}

	protected override void OnDrawGizmosSelected()
	{
		if (base.EditorShowGizmos)
		{
			this._worldspacePropsBounds = this.TransformBounds(this.PropsBounds);
		}
		base.OnDrawGizmosSelected();
		if (base.EditorShowGizmos && this.ActivationBounds != null)
		{
			Gizmos.color = Color.blue;
			Bounds[] activationBounds = this.ActivationBounds;
			foreach (Bounds bounds in activationBounds)
			{
				Bounds bounds2 = this.TransformBounds(bounds);
				Gizmos.DrawWireCube(bounds2.center, bounds2.size);
			}
		}
	}

	protected override void OnVisibilityChanged(bool isVisible)
	{
		this._autoCuller.SetVisibility(isVisible);
		foreach (CullableArea childArea in this._childAreas)
		{
			childArea.SetVisibility(isVisible && childArea.ShouldBeVisible);
		}
	}

	protected override void UpdateVisible()
	{
		base.UpdateVisible();
		foreach (CullableArea childArea in this._childAreas)
		{
			childArea.SetVisibility(childArea.ShouldBeVisible);
		}
	}
}

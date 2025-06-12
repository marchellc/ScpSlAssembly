using System;
using System.Collections.Generic;
using MapGeneration;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace ProgressiveCulling;

public class CullableRoom : CullableBehaviour, IRootCullable, ICullable, IBoundsCullable
{
	public static readonly List<CullableRoom> ActiveRooms = new List<CullableRoom>();

	private readonly AutoCuller _autoCuller = new AutoCuller();

	private readonly List<ICullable> _subCullables = new List<ICullable>();

	private readonly HashSet<GameObject> _originalActiveChildren = new HashSet<GameObject>();

	private RoomIdentifier _roomIdentifier;

	private bool _cached;

	public Bounds WorldspaceBounds { get; private set; }

	public RootCullablePriority Priority => RootCullablePriority.Rooms;

	public override bool ShouldBeVisible
	{
		get
		{
			if (this._cached && RoomPreCuller.ValidateCoords(this._roomIdentifier.MainCoords))
			{
				return CullingCamera.CheckBoundsVisibility(this.WorldspaceBounds);
			}
			return false;
		}
	}

	public event Action OnVisibilityUpdated;

	public void AddChildCullable(ICullable cullable)
	{
		if (this._subCullables.AddIfNotContains(cullable) && cullable is IBoundsCullable boundsCullable)
		{
			this.WorldspaceBounds.Encapsulate(boundsCullable.WorldspaceBounds);
		}
	}

	public void RemoveChildSullable(ICullable cullable)
	{
		this._subCullables.Remove(cullable);
	}

	public void SetupCache()
	{
		this._autoCuller.Generate(base.gameObject, AllowCullingFilter, CullingMath.GetSafeForDeactivation);
		Bounds worldspaceBounds = this._roomIdentifier.WorldspaceBounds;
		foreach (ICullable subCullable in this._subCullables)
		{
			if (!(subCullable is IRootCullable) && subCullable is IBoundsCullable boundsCullable)
			{
				worldspaceBounds.Encapsulate(boundsCullable.WorldspaceBounds);
			}
		}
		this._cached = true;
		this.WorldspaceBounds = worldspaceBounds;
		this.OnVisibilityChanged(this.ShouldBeVisible);
	}

	protected override void OnVisibilityChanged(bool isVisible)
	{
		this._autoCuller.SetVisibility(isVisible);
		if (isVisible)
		{
			this.UpdateVisible();
			CullableRoom.ActiveRooms.Add(this);
		}
		else
		{
			foreach (ICullable subCullable in this._subCullables)
			{
				if (!this.IsNullSafe(subCullable))
				{
					subCullable.SetVisibility(isVisible: false);
				}
			}
			CullableRoom.ActiveRooms.Remove(this);
		}
		this.OnVisibilityUpdated?.Invoke();
	}

	protected override void UpdateVisible()
	{
		base.UpdateVisible();
		for (int num = this._subCullables.Count - 1; num >= 0; num--)
		{
			ICullable cullable = this._subCullables[num];
			if (this.IsNullSafe(cullable))
			{
				this._subCullables.RemoveAt(num);
			}
			else
			{
				cullable.UpdateState();
			}
		}
	}

	private void ProcessOriginalChild(GameObject go)
	{
		if (go.activeSelf)
		{
			this._originalActiveChildren.Add(go);
			Transform transform = go.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				this.ProcessOriginalChild(transform.GetChild(i).gameObject);
			}
		}
	}

	private bool IsNullSafe(ICullable cull)
	{
		if (cull != null)
		{
			if (cull is Component component)
			{
				return component == null;
			}
			return false;
		}
		return true;
	}

	private void Awake()
	{
		this._roomIdentifier = base.GetComponent<RoomIdentifier>();
		this.ProcessOriginalChild(base.gameObject);
		CullingCamera.RegisterRootCullable(this);
	}

	private void OnDestroy()
	{
		CullingCamera.UnregisterRootCullable(this);
		CullableRoom.ActiveRooms.Remove(this);
	}

	private bool AllowCullingFilter(GameObject go)
	{
		if (this._originalActiveChildren.Contains(go))
		{
			if (go.TryGetComponent<ICullable>(out var component))
			{
				return !this._subCullables.Contains(component);
			}
			return true;
		}
		return false;
	}
}

using System;
using System.Collections.Generic;
using MapGeneration;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace ProgressiveCulling
{
	public class CullableRoom : CullableBehaviour, IRootCullable, ICullable, IBoundsCullable
	{
		public event Action OnVisibilityUpdated;

		public Bounds WorldspaceBounds { get; private set; }

		public RootCullablePriority Priority
		{
			get
			{
				return RootCullablePriority.Rooms;
			}
		}

		public override bool ShouldBeVisible
		{
			get
			{
				Vector3Int vector3Int;
				return this._cached && this._roomIdentifier.TryGetMainCoords(out vector3Int) && RoomPreCuller.ValidateCoords(vector3Int) && CullingCamera.CheckBoundsVisibility(this.WorldspaceBounds);
			}
		}

		public void AddChildCullable(ICullable cullable)
		{
			if (!this._subCullables.AddIfNotContains(cullable))
			{
				return;
			}
			IBoundsCullable boundsCullable = cullable as IBoundsCullable;
			if (boundsCullable == null)
			{
				return;
			}
			this.WorldspaceBounds.Encapsulate(boundsCullable.WorldspaceBounds);
		}

		public void RemoveChildSullable(ICullable cullable)
		{
			this._subCullables.Remove(cullable);
		}

		public void SetupCache()
		{
			this._autoCuller.Generate(base.gameObject, new Predicate<GameObject>(this.AllowCullingFilter), new Predicate<GameObject>(CullingMath.GetSafeForDeactivation), false);
			Bounds bounds = new Bounds(base.transform.position, Vector3.zero);
			foreach (GameObject gameObject in this._originalActiveChildren)
			{
				MeshRenderer meshRenderer;
				if (!(gameObject == null) && gameObject.TryGetComponent<MeshRenderer>(out meshRenderer))
				{
					meshRenderer.ResetBounds();
					bounds.Encapsulate(meshRenderer.bounds);
				}
			}
			foreach (ICullable cullable in this._subCullables)
			{
				if (!(cullable is IRootCullable))
				{
					IBoundsCullable boundsCullable = cullable as IBoundsCullable;
					if (boundsCullable != null)
					{
						bounds.Encapsulate(boundsCullable.WorldspaceBounds);
					}
				}
			}
			this._cached = true;
			this.WorldspaceBounds = bounds;
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
				foreach (ICullable cullable in this._subCullables)
				{
					if (!this.IsNullSafe(cullable))
					{
						cullable.SetVisibility(false);
					}
				}
				CullableRoom.ActiveRooms.Remove(this);
			}
			Action onVisibilityUpdated = this.OnVisibilityUpdated;
			if (onVisibilityUpdated == null)
			{
				return;
			}
			onVisibilityUpdated();
		}

		protected override void UpdateVisible()
		{
			base.UpdateVisible();
			for (int i = this._subCullables.Count - 1; i >= 0; i--)
			{
				ICullable cullable = this._subCullables[i];
				if (this.IsNullSafe(cullable))
				{
					this._subCullables.RemoveAt(i);
				}
				else
				{
					cullable.UpdateState();
				}
			}
		}

		private void ProcessOriginalChild(GameObject go)
		{
			if (!go.activeSelf)
			{
				return;
			}
			this._originalActiveChildren.Add(go);
			Transform transform = go.transform;
			for (int i = 0; i < transform.childCount; i++)
			{
				this.ProcessOriginalChild(transform.GetChild(i).gameObject);
			}
		}

		private bool IsNullSafe(ICullable cull)
		{
			if (cull != null)
			{
				Component component = cull as Component;
				return component != null && component == null;
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
			ICullable cullable;
			return this._originalActiveChildren.Contains(go) && (!go.TryGetComponent<ICullable>(out cullable) || !this._subCullables.Contains(cullable));
		}

		public static readonly List<CullableRoom> ActiveRooms = new List<CullableRoom>();

		private readonly AutoCuller _autoCuller = new AutoCuller();

		private readonly List<ICullable> _subCullables = new List<ICullable>();

		private readonly HashSet<GameObject> _originalActiveChildren = new HashSet<GameObject>();

		private RoomIdentifier _roomIdentifier;

		private bool _cached;
	}
}

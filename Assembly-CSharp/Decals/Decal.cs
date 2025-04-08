using System;
using ProgressiveCulling;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Decals
{
	[RequireComponent(typeof(DecalProjector))]
	public class Decal : CullableBehaviour
	{
		public Color InstancedColor
		{
			get
			{
				return this._instancedColor;
			}
			set
			{
				if (this._materialInstance == null)
				{
					this._materialInstance = new Material(this._projector.material);
					this._projector.material = this._materialInstance;
				}
				this._instancedColor = value;
				this._projector.material.SetColor("_BaseColor", this._instancedColor);
			}
		}

		public Material DecalMaterial
		{
			get
			{
				return this._projector.material;
			}
			set
			{
				this._projector.material = value;
			}
		}

		public Vector3 DecalSize
		{
			get
			{
				return this._projector.size;
			}
			set
			{
				this._projector.size = value;
			}
		}

		public Vector2 UVOffset
		{
			get
			{
				return this._projector.uvBias;
			}
			set
			{
				this._projector.uvBias = value;
			}
		}

		public Vector2 UVTiling
		{
			get
			{
				return this._projector.uvScale;
			}
			set
			{
				this._projector.uvScale = value;
			}
		}

		public Transform CachedTransform { get; private set; }

		public override bool ShouldBeVisible
		{
			get
			{
				return CullingCamera.CheckBoundsVisibility(new Bounds(this.CachedTransform.position, this.DecalSize));
			}
		}

		public virtual void AttachToSurface(RaycastHit hitSurface)
		{
			Transform transform = hitSurface.collider.transform;
			this.CachedTransform.SetParent(transform);
			this.CachedTransform.SetPositionAndRotation(hitSurface.point + hitSurface.normal * 0.02f, Quaternion.LookRotation(-hitSurface.normal));
			if (transform.TryGetComponentInParent(out this._parentCullableRoom))
			{
				this._parentCullableRoom.AddChildCullable(this);
				this._attachedToSurface = true;
			}
			this._gridSettings.ProcessAttachment();
			if (this._sizeMaxRandomMultiplier > 0f)
			{
				this.DecalSize = this._defaultSize * (global::UnityEngine.Random.value * this._sizeMaxRandomMultiplier + 1f);
			}
		}

		public virtual void Detach()
		{
			if (!this._attachedToSurface)
			{
				return;
			}
			this._attachedToSurface = false;
			if (this._parentCullableRoom != null)
			{
				this._parentCullableRoom.RemoveChildSullable(this);
			}
		}

		public void SetRandomRotation()
		{
			this.CachedTransform.Rotate(0f, 0f, global::UnityEngine.Random.value * 360f, Space.Self);
		}

		protected virtual void Awake()
		{
			this._projector = base.GetComponent<DecalProjector>();
			this.CachedTransform = base.transform;
			this._gridSettings.SetReference(this);
			this._defaultSize = this.DecalSize;
		}

		protected virtual void OnEnable()
		{
			this._projector.enabled = true;
		}

		protected virtual void OnDisable()
		{
			this._projector.enabled = false;
		}

		protected override void OnVisibilityChanged(bool isVisible)
		{
			base.enabled = isVisible;
		}

		private DecalProjector _projector;

		private CullableRoom _parentCullableRoom;

		private bool _attachedToSurface;

		private const float SurfaceDistance = 0.02f;

		private Color _instancedColor;

		private Material _materialInstance;

		private Vector3 _defaultSize;

		[SerializeField]
		private Decal.GridSettings _gridSettings;

		[SerializeField]
		private float _sizeMaxRandomMultiplier;

		public DecalPoolType DecalPoolType;

		[Serializable]
		private class GridSettings
		{
			public void SetReference(Decal decal)
			{
				this._decalRef = decal;
			}

			public void ProcessAttachment()
			{
				if (!this.Enabled)
				{
					return;
				}
				int num = global::UnityEngine.Random.Range(0, this.DecalsNum);
				int num2 = num % this.GridSize;
				int num3 = num / this.GridSize;
				num3 = this.GridSize - 1 - num3;
				this._decalRef.UVOffset = new Vector2((float)num2, (float)num3) / (float)this.GridSize;
				if (this._tilingSet)
				{
					return;
				}
				this._decalRef.UVTiling = Vector2.one / (float)this.GridSize;
				this._tilingSet = true;
			}

			public bool Enabled;

			public int GridSize;

			public int DecalsNum;

			private Decal _decalRef;

			private bool _tilingSet;
		}
	}
}

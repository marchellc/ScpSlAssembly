using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace ProgressiveCulling
{
	public class CullingCamera : MonoBehaviour
	{
		public static event Action<RootCullablePriority, Camera> OnStageStarted;

		public static Vector3 LastCamPosition { get; private set; }

		private static bool CullingPaused
		{
			get
			{
				if (!CullingCamera._pauseCulling)
				{
					return false;
				}
				ReferenceHub referenceHub;
				if (!ReferenceHub.TryGetLocalHub(out referenceHub) || !referenceHub.IsAlive())
				{
					return true;
				}
				foreach (ReferenceHub referenceHub2 in ReferenceHub.AllHubs)
				{
					if (referenceHub2.IsAlive() && HitboxIdentity.IsEnemy(referenceHub2, referenceHub))
					{
						return false;
					}
				}
				return true;
			}
		}

		private void Awake()
		{
			this._camera = base.GetComponent<Camera>();
		}

		private void OnEnable()
		{
			MainCameraController.OnUpdated += this.UpdateCulling;
		}

		private void OnDisable()
		{
			MainCameraController.OnUpdated -= this.UpdateCulling;
		}

		private void UpdateCulling()
		{
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationStage += CullingCamera.OnMapGenerationStage;
		}

		private static void OnMapGenerationStage(MapGenerationPhase stage)
		{
			if (stage != MapGenerationPhase.CullingCaching)
			{
				return;
			}
			foreach (IRootCullable rootCullable in CullingCamera.RootCullables)
			{
				rootCullable.SetupCache();
			}
		}

		public static bool CheckBoundsVisibility(Plane[] planes, float maxDistanceSqr, Bounds bounds)
		{
			Vector3 min = bounds.min;
			Vector3 max = bounds.max;
			foreach (Plane plane in planes)
			{
				Vector3 normal = plane.normal;
				Vector3 vector = min;
				Vector3 vector2 = max;
				if (normal.x >= 0f)
				{
					vector.x = max.x;
					vector2.x = min.x;
				}
				if (normal.y >= 0f)
				{
					vector.y = max.y;
					vector2.y = min.y;
				}
				if (normal.z >= 0f)
				{
					vector.z = max.z;
					vector2.z = min.z;
				}
				if (Vector3.Dot(normal, vector) + plane.distance < 0f)
				{
					return false;
				}
			}
			return bounds.SqrDistance(CullingCamera.LastCamPosition) < maxDistanceSqr;
		}

		public static bool CheckBoundsVisibility(Bounds bounds)
		{
			return CullingCamera.LastPlanes == null || CullingCamera.CheckBoundsVisibility(CullingCamera.LastPlanes, CullingCamera._lastFarPlaneSqr, bounds);
		}

		public static void RegisterRootCullable(IRootCullable cullable)
		{
			CullingCamera.RootCullables.Add(cullable);
			if (SeedSynchronizer.MapGenerated)
			{
				cullable.SetupCache();
			}
		}

		public static void UnregisterRootCullable(IRootCullable cullable)
		{
			CullingCamera.RootCullables.Remove(cullable);
		}

		public static bool TogglePause()
		{
			CullingCamera._pauseCulling = !CullingCamera._pauseCulling;
			return CullingCamera.CullingPaused;
		}

		private Camera _camera;

		[SerializeField]
		private float _minFarClipPlane = 10f;

		[SerializeField]
		private float _farClipPlaneFogOffset = 1f;

		private static float _lastFarPlaneSqr;

		private static bool _pauseCulling;

		private static readonly Plane[] LastPlanes = new Plane[6];

		private static readonly HashSet<IRootCullable> RootCullables = new HashSet<IRootCullable>();
	}
}

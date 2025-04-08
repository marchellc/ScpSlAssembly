using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;

namespace ProgressiveCulling
{
	public static class CullingMath
	{
		public static Plane[] CalculateFrustumPlanesAspectRatio(float verticalFov, float aspectRatio, float zNear, float zFar, Vector3 pos, Quaternion rot)
		{
			Matrix4x4 matrix4x = Matrix4x4.Perspective(verticalFov, aspectRatio, zNear, zFar);
			Matrix4x4 inverse = Matrix4x4.TRS(pos, rot * Quaternion.Euler(0f, 180f, 0f), Vector3.one).inverse;
			GeometryUtility.CalculateFrustumPlanes(matrix4x * inverse, CullingMath.CustomPlanesNonAlloc);
			return CullingMath.CustomPlanesNonAlloc;
		}

		public static Plane[] CalculateFrustumPlanesFovs(float verticalFov, float horizontalFov, float zNear, float zFar, Vector3 pos, Quaternion rot)
		{
			float num = Mathf.Tan(0.017453292f * horizontalFov / 2f);
			float num2 = Mathf.Tan(0.017453292f * verticalFov / 2f);
			return CullingMath.CalculateFrustumPlanesAspectRatio(verticalFov, num / num2, zNear, zFar, pos, rot);
		}

		public static float VerticalToHorizontalFov(float fov, float aspect)
		{
			float num = fov * 0.017453292f;
			float num2 = 2f * Mathf.Atan(Mathf.Tan(num / 2f) * aspect);
			return 57.29578f * num2;
		}

		public static bool Intersect2DRayAABB(Vector2 min, Vector2 max, Ray2D ray)
		{
			Vector2 direction = ray.direction;
			Vector2 origin = ray.origin;
			float num = float.NegativeInfinity;
			float num2 = float.PositiveInfinity;
			for (int i = 0; i < 2; i++)
			{
				float num3 = direction[i];
				float num4 = origin[i];
				if (num3 != 0f)
				{
					float num5 = (min[i] - num4) / num3;
					float num6 = (max[i] - num4) / num3;
					float num7 = Mathf.Min(num5, num6);
					float num8 = Mathf.Max(num5, num6);
					num = Mathf.Max(num, num7);
					num2 = Mathf.Min(num2, num8);
					if (num2 < num)
					{
						return false;
					}
				}
				else if (num4 < min[i] || num4 > max[i])
				{
					return false;
				}
			}
			return num2 >= 0f;
		}

		public static bool ContainsBoundsWithinVertex(VertexAngle2D vertex, Bounds worldspaceBounds)
		{
			Vector2 vector = CullingMath.To2D(worldspaceBounds.min);
			Vector2 vector2 = CullingMath.To2D(worldspaceBounds.max);
			return CullingMath.ContainsPointWithinVertex(vertex, vector) || CullingMath.ContainsPointWithinVertex(vertex, vector2) || CullingMath.ContainsPointWithinVertex(vertex, new Vector2(vector.x, vector2.y)) || CullingMath.ContainsPointWithinVertex(vertex, new Vector2(vector2.x, vector.y));
		}

		public static bool ContainsPointWithinVertex(VertexAngle2D vertex, Vector2 point)
		{
			Vector2 vector = point - vertex.Origin;
			float num = CullingMath.<ContainsPointWithinVertex>g__Cross|7_0(vertex.Left, vector);
			float num2 = CullingMath.<ContainsPointWithinVertex>g__Cross|7_0(vertex.Right, vector);
			return num >= 0f && num2 <= 0f;
		}

		public static Vector3 To3D(Vector2 v2, float height)
		{
			return new Vector3(v2.x, height, v2.y);
		}

		public static Vector2 To2D(Vector3 v3)
		{
			return new Vector2(v3.x, v3.z);
		}

		public static bool TryGetCommon(VertexAngle2D a, VertexAngle2D b, out VertexAngle2D result)
		{
			if (!CullingMath.<TryGetCommon>g__Ordered|10_0(a.Left, a.Right))
			{
				ref Vector2 ptr = ref a.Right;
				Vector2 vector = a.Left;
				Vector2 vector2 = a.Right;
				ptr = vector;
				a.Left = vector2;
			}
			if (!CullingMath.<TryGetCommon>g__Ordered|10_0(b.Left, b.Right))
			{
				ref Vector2 ptr = ref b.Right;
				Vector2 vector2 = b.Left;
				Vector2 vector = b.Right;
				ptr = vector2;
				b.Left = vector;
			}
			Vector2 vector3 = (CullingMath.<TryGetCommon>g__Ordered|10_0(a.Left, b.Left) ? b.Left : a.Left);
			Vector2 vector4 = (CullingMath.<TryGetCommon>g__Ordered|10_0(a.Right, b.Right) ? a.Right : b.Right);
			if (CullingMath.<TryGetCommon>g__Ordered|10_0(vector3, vector4))
			{
				result = new VertexAngle2D(a.Origin, vector3, vector4);
				return true;
			}
			result = default(VertexAngle2D);
			return false;
		}

		public static bool GetSafeForDeactivation(GameObject go)
		{
			go.GetComponentsInChildren<Component>(true, CullingMath.ComponentFetcherNonAlloc);
			bool flag = false;
			foreach (Component component in CullingMath.ComponentFetcherNonAlloc)
			{
				if (component is NetworkIdentity || component is AudioSource || component is Collider || component is SpawnablesDistributorBase || component is Animator)
				{
					return false;
				}
				if (component is Renderer)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				return false;
			}
			go.GetComponentsInParent<Component>(true, CullingMath.ComponentFetcherNonAlloc);
			foreach (Component component2 in CullingMath.ComponentFetcherNonAlloc)
			{
				if (component2 is Clutter || component2 is BreakableWindow)
				{
					return false;
				}
			}
			return true;
		}

		[CompilerGenerated]
		internal static float <ContainsPointWithinVertex>g__Cross|7_0(Vector2 a, Vector2 b)
		{
			return a.x * b.y - a.y * b.x;
		}

		[CompilerGenerated]
		internal static bool <TryGetCommon>g__Ordered|10_0(Vector2 left, Vector2 right)
		{
			return left.x * right.y - left.y * right.x >= 0f;
		}

		private static readonly Plane[] CustomPlanesNonAlloc = new Plane[6];

		private static readonly List<Component> ComponentFetcherNonAlloc = new List<Component>();
	}
}

using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace ProgressiveCulling
{
	public static class RoomPreCuller
	{
		private static float RoomSize
		{
			get
			{
				float num = RoomPreCuller._roomSize.GetValueOrDefault();
				if (RoomPreCuller._roomSize == null)
				{
					num = Mathf.Max(RoomIdentifier.GridScale.x, RoomIdentifier.GridScale.z);
					RoomPreCuller._roomSize = new float?(num);
				}
				return RoomPreCuller._roomSize.Value;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CullingCamera.OnStageStarted += RoomPreCuller.OnStageStarted;
		}

		private static void OnStageStarted(RootCullablePriority priority, Camera cam)
		{
			if (priority != RootCullablePriority.PreCull)
			{
				return;
			}
			RoomPreCuller._lastSuccess = RoomPreCuller.TryPreCull(cam);
		}

		private static bool TryPreCull(Camera cam)
		{
			ReferenceHub referenceHub;
			if (ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				IFpcRole fpcRole = referenceHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null && fpcRole.FpcModule.Noclip.IsActive)
				{
					return false;
				}
			}
			Transform transform = cam.transform;
			RoomPreCuller._lastCameraPosition = transform.position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(RoomPreCuller._lastCameraPosition, true);
			Vector3Int vector3Int;
			if (roomIdentifier == null || !roomIdentifier.TryGetMainCoords(out vector3Int))
			{
				return false;
			}
			RoomPreCuller.VisibleCoords.Clear();
			int num = Mathf.CeilToInt((cam.farClipPlane + 4f) / RoomPreCuller.RoomSize);
			VertexAngle2D vertexAngle2D = new VertexAngle2D(transform.forward, RoomPreCuller._lastCameraPosition, cam.fieldOfView, cam.aspect);
			RoomPreCuller.ScanCoords(vector3Int, vertexAngle2D, num, 0);
			return true;
		}

		private static void ScanCoords(Vector3Int coords, VertexAngle2D camVertex, int maxJumps, int iteration)
		{
			RoomPreCuller.DebugVertexAngle(camVertex, (float)iteration * RoomPreCuller.RoomSize + 1f);
			RoomPreCuller.VisibleCoords.Add(coords);
			if (iteration >= maxJumps)
			{
				return;
			}
			foreach (Vector3Int vector3Int in RoomPreCuller.ScanDirections)
			{
				Vector3Int vector3Int2 = coords + vector3Int;
				RoomCullingConnection roomCullingConnection;
				if (!RoomPreCuller.VisibleCoords.Contains(vector3Int2) && RoomCullingConnection.TryGetConnection(coords, vector3Int2, out roomCullingConnection))
				{
					Bounds worldspaceBounds = roomCullingConnection.WorldspaceBounds;
					if (roomCullingConnection.Connector.IsVisibleThrough || worldspaceBounds.Contains(RoomPreCuller._lastCameraPosition))
					{
						VertexAngle2D vertexAngle2D = new VertexAngle2D(camVertex.Origin, worldspaceBounds);
						VertexAngle2D vertexAngle2D2;
						bool flag;
						if (iteration == 0)
						{
							vertexAngle2D2 = vertexAngle2D;
							flag = true;
						}
						else
						{
							flag = CullingMath.TryGetCommon(camVertex, vertexAngle2D, out vertexAngle2D2);
						}
						if (flag && CullingCamera.CheckBoundsVisibility(worldspaceBounds))
						{
							RoomPreCuller.ScanCoords(vector3Int2, vertexAngle2D2, maxJumps, iteration + 1);
						}
						else
						{
							RoomPreCuller.ScanBounds(roomCullingConnection, camVertex, vector3Int2);
						}
					}
				}
			}
		}

		private static void ScanBounds(RoomCullingConnection rcc, VertexAngle2D vertex, Vector3Int otherCoords)
		{
			RoomCullingConnection.RoomLink link = rcc.Link;
			CullableRoom cullableRoom;
			if (link.Coords.CoordsA == otherCoords)
			{
				cullableRoom = link.CullableA;
			}
			else
			{
				if (!(link.Coords.CoordsB == otherCoords))
				{
					Debug.LogError(string.Format("Link {0} is not matching any rooms at {1}.", rcc.name, otherCoords));
					return;
				}
				cullableRoom = link.CullableB;
			}
			Bounds worldspaceBounds = cullableRoom.WorldspaceBounds;
			if (!CullingCamera.CheckBoundsVisibility(worldspaceBounds))
			{
				return;
			}
			if (!CullingMath.ContainsBoundsWithinVertex(vertex, worldspaceBounds))
			{
				return;
			}
			RoomPreCuller.VisibleCoords.Add(otherCoords);
		}

		private static void DebugVertexAngle(VertexAngle2D vertex, float distance)
		{
			if (!RoomPreCuller.EnableDebugging)
			{
				return;
			}
			Vector3 vector = CullingMath.To3D(vertex.Origin, RoomPreCuller._lastCameraPosition.y);
			Vector3 vector2 = CullingMath.To3D(vertex.Left, 0f);
			Vector3 vector3 = CullingMath.To3D(vertex.Right, 0f);
			Debug.DrawRay(vector, vector2 * distance, Color.green);
			Debug.DrawRay(vector, vector3 * distance, Color.yellow);
		}

		public static bool ValidateCoords(Vector3Int coords)
		{
			return !RoomPreCuller._lastSuccess || RoomPreCuller.VisibleCoords.Contains(coords);
		}

		private static readonly HashSet<Vector3Int> VisibleCoords = new HashSet<Vector3Int>();

		private static bool _lastSuccess = false;

		private static Vector3 _lastCameraPosition;

		private static float? _roomSize;

		private const float ExtraCameraRange = 4f;

		private static readonly Vector3Int[] ScanDirections = new Vector3Int[]
		{
			Vector3Int.forward,
			Vector3Int.back,
			Vector3Int.left,
			Vector3Int.right
		};

		private static readonly bool EnableDebugging = false;
	}
}

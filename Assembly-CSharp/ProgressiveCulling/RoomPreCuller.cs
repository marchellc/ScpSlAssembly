using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace ProgressiveCulling;

public static class RoomPreCuller
{
	private static readonly HashSet<Vector3Int> VisibleCoords = new HashSet<Vector3Int>();

	private static bool _lastSuccess = false;

	private static Vector3 _lastCameraPosition;

	private static float? _roomSize;

	private const float ExtraCameraRange = 4f;

	private static readonly Vector3Int[] ScanDirections = new Vector3Int[4]
	{
		Vector3Int.forward,
		Vector3Int.back,
		Vector3Int.left,
		Vector3Int.right
	};

	private static readonly bool EnableDebugging = false;

	private static float RoomSize
	{
		get
		{
			float valueOrDefault = RoomPreCuller._roomSize.GetValueOrDefault();
			if (!RoomPreCuller._roomSize.HasValue)
			{
				valueOrDefault = Mathf.Max(RoomIdentifier.GridScale.x, RoomIdentifier.GridScale.z);
				RoomPreCuller._roomSize = valueOrDefault;
			}
			return RoomPreCuller._roomSize.Value;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CullingCamera.OnStageStarted += OnStageStarted;
	}

	private static void OnStageStarted(RootCullablePriority priority, Camera cam)
	{
		if (priority == RootCullablePriority.PreCull)
		{
			RoomPreCuller._lastSuccess = RoomPreCuller.TryPreCull(cam);
		}
	}

	private static bool TryPreCull(Camera cam)
	{
		if (ReferenceHub.TryGetLocalHub(out var hub) && hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule.Noclip.IsActive)
		{
			return false;
		}
		Transform transform = cam.transform;
		RoomPreCuller._lastCameraPosition = transform.position;
		if (!RoomPreCuller._lastCameraPosition.TryGetRoom(out var room))
		{
			return false;
		}
		RoomPreCuller.VisibleCoords.Clear();
		int maxJumps = Mathf.CeilToInt((cam.farClipPlane + 4f) / RoomPreCuller.RoomSize);
		RoomPreCuller.ScanCoords(camVertex: new VertexAngle2D(transform.forward, RoomPreCuller._lastCameraPosition, cam.fieldOfView, cam.aspect), coords: room.MainCoords, maxJumps: maxJumps, iteration: 0);
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
		Vector3Int[] scanDirections = RoomPreCuller.ScanDirections;
		foreach (Vector3Int vector3Int in scanDirections)
		{
			Vector3Int vector3Int2 = coords + vector3Int;
			if (RoomPreCuller.VisibleCoords.Contains(vector3Int2) || !RoomCullingConnection.TryGetConnection(coords, vector3Int2, out var conn))
			{
				continue;
			}
			Bounds worldspaceBounds = conn.WorldspaceBounds;
			if (conn.Connector.IsVisibleThrough || worldspaceBounds.Contains(RoomPreCuller._lastCameraPosition))
			{
				VertexAngle2D vertexAngle2D = new VertexAngle2D(camVertex.Origin, worldspaceBounds);
				VertexAngle2D result;
				bool flag;
				if (iteration == 0)
				{
					result = vertexAngle2D;
					flag = true;
				}
				else
				{
					flag = CullingMath.TryGetCommon(camVertex, vertexAngle2D, out result);
				}
				if (flag && CullingCamera.CheckBoundsVisibility(worldspaceBounds))
				{
					RoomPreCuller.ScanCoords(vector3Int2, result, maxJumps, iteration + 1);
				}
				else
				{
					RoomPreCuller.ScanBounds(conn, camVertex, vector3Int2);
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
				Debug.LogError($"Link {rcc.name} is not matching any rooms at {otherCoords}.");
				return;
			}
			cullableRoom = link.CullableB;
		}
		Bounds worldspaceBounds = cullableRoom.WorldspaceBounds;
		if (CullingCamera.CheckBoundsVisibility(worldspaceBounds) && CullingMath.ContainsBoundsWithinVertex(vertex, worldspaceBounds))
		{
			RoomPreCuller.VisibleCoords.Add(otherCoords);
		}
	}

	private static void DebugVertexAngle(VertexAngle2D vertex, float distance)
	{
		if (RoomPreCuller.EnableDebugging)
		{
			Vector3 start = CullingMath.To3D(vertex.Origin, RoomPreCuller._lastCameraPosition.y);
			Vector3 vector = CullingMath.To3D(vertex.Left, 0f);
			Vector3 vector2 = CullingMath.To3D(vertex.Right, 0f);
			Debug.DrawRay(start, vector * distance, Color.green);
			Debug.DrawRay(start, vector2 * distance, Color.yellow);
		}
	}

	public static bool ValidateCoords(Vector3Int coords)
	{
		if (RoomPreCuller._lastSuccess)
		{
			return RoomPreCuller.VisibleCoords.Contains(coords);
		}
		return true;
	}
}

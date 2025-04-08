using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;

public static class SafeLocationFinder
{
	public static List<Pose> GetLocations(Predicate<RoomCullingConnection> connectionFilter, Predicate<DoorVariant> doorFilter)
	{
		SafeLocationFinder.ResultsNonAlloc.Clear();
		SafeLocationFinder.GetLocations(SafeLocationFinder.ResultsNonAlloc, connectionFilter, doorFilter);
		return SafeLocationFinder.ResultsNonAlloc;
	}

	public static void GetLocations(List<Pose> results, Predicate<RoomCullingConnection> connectionFilter, Predicate<DoorVariant> doorFilter)
	{
		SafeLocationFinder.DuplicatePreventer.Clear();
		foreach (RoomCullingConnection roomCullingConnection in RoomCullingConnection.AllInstances)
		{
			if (connectionFilter == null || connectionFilter(roomCullingConnection))
			{
				SafeLocationFinder.AddTransform(roomCullingConnection.transform, results);
			}
		}
		foreach (DoorVariant doorVariant in DoorVariant.AllDoors)
		{
			if (doorFilter == null || doorFilter(doorVariant))
			{
				SafeLocationFinder.AddTransform(doorVariant.transform, results);
			}
		}
	}

	public static Vector3 GetSafePosition(Vector3 position, Vector3 direction, float range, CharacterController charController)
	{
		direction = Quaternion.Euler(0f, global::UnityEngine.Random.Range(-30f, 30f), 0f) * direction;
		float radius = charController.radius;
		float num = Mathf.Lerp(radius, range, global::UnityEngine.Random.value);
		Vector3 vector = Vector3.up * charController.height / 2f;
		Vector3 vector2 = position + charController.center + SafeLocationFinder.GroundOffset + Vector3.up * radius;
		RaycastHit raycastHit;
		if (!Physics.SphereCast(vector2, radius, direction, out raycastHit, num + radius, FpcStateProcessor.Mask))
		{
			return vector2 + direction * num + vector;
		}
		return raycastHit.point + raycastHit.normal * radius + vector;
	}

	public static Vector3 GetSafePositionForPose(Pose pose, float range, CharacterController charController, bool randomizeDir = true)
	{
		Vector3 vector = pose.forward;
		if (randomizeDir && global::UnityEngine.Random.value > 0.5f)
		{
			vector = -vector;
		}
		return SafeLocationFinder.GetSafePosition(pose.position, vector, range, charController);
	}

	private static void AddTransform(Transform tr, List<Pose> target)
	{
		Vector3 vector;
		Quaternion quaternion;
		tr.GetPositionAndRotation(out vector, out quaternion);
		Pose pose = new Pose(vector, quaternion);
		if (!SafeLocationFinder.DuplicatePreventer.Add(pose))
		{
			return;
		}
		target.Add(pose);
	}

	private static readonly List<Pose> ResultsNonAlloc = new List<Pose>();

	private static readonly HashSet<Pose> DuplicatePreventer = new HashSet<Pose>();

	private static readonly Vector3 GroundOffset = new Vector3(0f, 0.25f, 0f);

	private const float AngleVariation = 30f;
}

using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;

public static class SafeLocationFinder
{
	private static readonly List<Pose> ResultsNonAlloc = new List<Pose>();

	private static readonly HashSet<Pose> DuplicatePreventer = new HashSet<Pose>();

	private static readonly Vector3 GroundOffset = new Vector3(0f, 0.25f, 0f);

	private const float AngleVariation = 30f;

	public static List<Pose> GetLocations(Predicate<RoomCullingConnection> connectionFilter, Predicate<DoorVariant> doorFilter)
	{
		SafeLocationFinder.ResultsNonAlloc.Clear();
		SafeLocationFinder.GetLocations(SafeLocationFinder.ResultsNonAlloc, connectionFilter, doorFilter);
		return SafeLocationFinder.ResultsNonAlloc;
	}

	public static void GetLocations(List<Pose> results, Predicate<RoomCullingConnection> connectionFilter, Predicate<DoorVariant> doorFilter)
	{
		SafeLocationFinder.DuplicatePreventer.Clear();
		foreach (RoomCullingConnection allInstance in RoomCullingConnection.AllInstances)
		{
			if (connectionFilter == null || connectionFilter(allInstance))
			{
				SafeLocationFinder.AddTransform(allInstance.transform, results);
			}
		}
		foreach (DoorVariant allDoor in DoorVariant.AllDoors)
		{
			if (doorFilter == null || doorFilter(allDoor))
			{
				SafeLocationFinder.AddTransform(allDoor.transform, results);
			}
		}
	}

	public static Vector3 GetSafePosition(Vector3 position, Vector3 direction, float range, CharacterController charController)
	{
		direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-30f, 30f), 0f) * direction;
		float radius = charController.radius;
		float num = Mathf.Lerp(radius, range, UnityEngine.Random.value);
		Vector3 vector = Vector3.up * charController.height / 2f;
		Vector3 vector2 = position + charController.center + SafeLocationFinder.GroundOffset + Vector3.up * radius;
		if (!Physics.SphereCast(vector2, radius, direction, out var hitInfo, num + radius, FpcStateProcessor.Mask))
		{
			return vector2 + direction * num + vector;
		}
		return hitInfo.point + hitInfo.normal * radius + vector;
	}

	public static Vector3 GetSafePositionForPose(Pose pose, float range, CharacterController charController, bool randomizeDir = true)
	{
		Vector3 vector = pose.forward;
		if (randomizeDir && UnityEngine.Random.value > 0.5f)
		{
			vector = -vector;
		}
		return SafeLocationFinder.GetSafePosition(pose.position, vector, range, charController);
	}

	private static void AddTransform(Transform tr, List<Pose> target)
	{
		tr.GetPositionAndRotation(out var position, out var rotation);
		Pose item = new Pose(position, rotation);
		if (SafeLocationFinder.DuplicatePreventer.Add(item))
		{
			target.Add(item);
		}
	}
}

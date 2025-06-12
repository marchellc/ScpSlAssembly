using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Interactables;
using Interactables.Interobjects;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using ProgressiveCulling;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp106;

public static class Scp106PocketExitFinder
{
	private const int RequiredTriggers = 2;

	private const int MaxArraySize = 64;

	private const int MaxDistanceSqr = 750;

	private const float ZombieSqrModifier = 0.3f;

	private const float RaycastRange = 11f;

	private const float SurfaceRaycastRange = 45f;

	public static readonly Dictionary<FacilityZone, Pose[]> PosesForZoneCache = new Dictionary<FacilityZone, Pose[]>();

	private static readonly RoomName[] BlacklistedRooms = new RoomName[6]
	{
		RoomName.Hcz079,
		RoomName.LczCheckpointA,
		RoomName.LczCheckpointB,
		RoomName.HczCheckpointToEntranceZone,
		RoomName.LczClassDSpawn,
		RoomName.HczTesla
	};

	private static readonly string[] BlacklistedDoors = new string[1] { "ESCAPE_FINAL" };

	private static readonly Pose[] PosesNonAlloc = new Pose[64];

	private static readonly Vector3[] PositionsCache = new Vector3[64];

	private static readonly bool[] PositionModifiers = new bool[64];

	private static readonly int Mask = LayerMask.GetMask("Default", "Glass");

	public static Vector3 GetBestExitPosition(IFpcRole role)
	{
		if (!NetworkServer.active)
		{
			throw new InvalidOperationException("Scp106PocketExitFinder.GetBestExitPosition is a server-side only method!");
		}
		if (!(role is PlayerRoleBase playerRoleBase) || !playerRoleBase.TryGetOwner(out var hub))
		{
			throw new InvalidOperationException("Scp106PocketExitFinder.GetBestExitPosition provided with non-compatible role!");
		}
		Vector3 position = hub.playerEffectsController.GetEffect<PocketCorroding>().CapturePosition.Position;
		if (!position.TryGetRoom(out var room))
		{
			return position;
		}
		Pose[] posesForZone = Scp106PocketExitFinder.GetPosesForZone(room.Zone);
		if (posesForZone.Length != 0)
		{
			Pose randomPose = Scp106PocketExitFinder.GetRandomPose(posesForZone);
			float range = ((room.Zone == FacilityZone.Surface) ? 45f : 11f);
			return SafeLocationFinder.GetSafePositionForPose(randomPose, range, role.FpcModule.CharController);
		}
		return position;
	}

	private static Pose GetRandomPose(Pose[] poses)
	{
		int num = 0;
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			if (allHub.roleManager.CurrentRole is FpcStandardScp fpcStandardScp)
			{
				Scp106PocketExitFinder.PositionsCache[num] = fpcStandardScp.FpcModule.Position;
				Scp106PocketExitFinder.PositionModifiers[num] = fpcStandardScp.RoleTypeId == RoleTypeId.Scp0492;
				if (++num >= 64)
				{
					break;
				}
			}
		}
		if (num == 0)
		{
			return poses.RandomItem();
		}
		Pose? pose = null;
		float num2 = float.MaxValue;
		int num3 = 0;
		for (int i = 0; i < poses.Length; i++)
		{
			Pose pose2 = poses[i];
			float num4 = 0f;
			bool flag = true;
			for (int j = 0; j < num; j++)
			{
				float num5 = (pose2.position - Scp106PocketExitFinder.PositionsCache[j]).sqrMagnitude;
				if (Scp106PocketExitFinder.PositionModifiers[j])
				{
					num5 *= 0.3f;
				}
				if (num5 < 750f)
				{
					flag = false;
				}
				num4 += num5;
			}
			if (flag)
			{
				Scp106PocketExitFinder.PosesNonAlloc[num3++] = pose2;
			}
			if (num4 < num2)
			{
				pose = pose2;
				num2 = num4;
			}
		}
		if (num3 != 0)
		{
			return Scp106PocketExitFinder.PosesNonAlloc[UnityEngine.Random.Range(0, num3)];
		}
		return pose ?? poses.RandomItem();
	}

	public static Pose[] GetPosesForZone(FacilityZone zone)
	{
		if (Scp106PocketExitFinder.PosesForZoneCache.TryGetValue(zone, out var value))
		{
			return value;
		}
		Pose[] array = SafeLocationFinder.GetLocations((RoomCullingConnection x) => Scp106PocketExitFinder.ValidateConnection(x, zone), (DoorVariant y) => Scp106PocketExitFinder.ValidateDoor(y, zone)).ToArray();
		Scp106PocketExitFinder.PosesForZoneCache[zone] = array;
		return array;
	}

	private static bool ValidateDoor(DoorVariant dv, FacilityZone requiredZone)
	{
		if (!(dv is BasicDoor basicDoor))
		{
			return false;
		}
		if (Scp106PocketExitFinder.BlacklistedDoors.Contains<string>(dv.DoorName))
		{
			return false;
		}
		if (dv.RequiredPermissions.RequiredPermissions != DoorPermissionFlags.None)
		{
			return false;
		}
		if (!InteractableCollider.AllInstances.TryGetValue(basicDoor, out var value))
		{
			return false;
		}
		if (value.Count < 2)
		{
			return false;
		}
		RoomIdentifier[] rooms = basicDoor.Rooms;
		for (int i = 0; i < rooms.Length; i++)
		{
			if (!Scp106PocketExitFinder.ValidateRoom(rooms[i], requiredZone))
			{
				return false;
			}
		}
		return true;
	}

	private static bool ValidateConnection(RoomCullingConnection conn, FacilityZone requiredZone)
	{
		RoomCullingConnection.RoomLink link = conn.Link;
		if (link.Valid && Scp106PocketExitFinder.ValidateRoom(link.RoomA, requiredZone))
		{
			return Scp106PocketExitFinder.ValidateRoom(link.RoomB, requiredZone);
		}
		return false;
	}

	private static bool ValidateRoom(RoomIdentifier room, FacilityZone requiredZone)
	{
		if (room.Zone == requiredZone)
		{
			return !Scp106PocketExitFinder.BlacklistedRooms.Contains(room.Name);
		}
		return false;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationFinished += Scp106PocketExitFinder.PosesForZoneCache.Clear;
	}
}

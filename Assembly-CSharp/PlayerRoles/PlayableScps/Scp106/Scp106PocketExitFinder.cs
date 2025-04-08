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

namespace PlayerRoles.PlayableScps.Scp106
{
	public static class Scp106PocketExitFinder
	{
		public static Vector3 GetBestExitPosition(IFpcRole role)
		{
			if (!NetworkServer.active)
			{
				throw new InvalidOperationException("Scp106PocketExitFinder.GetBestExitPosition is a server-side only method!");
			}
			PlayerRoleBase playerRoleBase = role as PlayerRoleBase;
			ReferenceHub referenceHub;
			if (playerRoleBase == null || !playerRoleBase.TryGetOwner(out referenceHub))
			{
				throw new InvalidOperationException("Scp106PocketExitFinder.GetBestExitPosition provided with non-compatible role!");
			}
			Vector3 position = referenceHub.playerEffectsController.GetEffect<PocketCorroding>().CapturePosition.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(position, true);
			if (roomIdentifier == null)
			{
				return position;
			}
			Pose[] posesForZone = Scp106PocketExitFinder.GetPosesForZone(roomIdentifier.Zone);
			if (posesForZone.Length != 0)
			{
				Pose randomPose = Scp106PocketExitFinder.GetRandomPose(posesForZone);
				float num = ((roomIdentifier.Zone == FacilityZone.Surface) ? 45f : 11f);
				return SafeLocationFinder.GetSafePositionForPose(randomPose, num, role.FpcModule.CharController, true);
			}
			return position;
		}

		private static Pose GetRandomPose(Pose[] poses)
		{
			int num = 0;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				FpcStandardScp fpcStandardScp = referenceHub.roleManager.CurrentRole as FpcStandardScp;
				if (fpcStandardScp != null)
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
				return poses.RandomItem<Pose>();
			}
			Pose? pose = null;
			float num2 = float.MaxValue;
			int num3 = 0;
			foreach (Pose pose2 in poses)
			{
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
					pose = new Pose?(pose2);
					num2 = num4;
				}
			}
			if (num3 != 0)
			{
				return Scp106PocketExitFinder.PosesNonAlloc[global::UnityEngine.Random.Range(0, num3)];
			}
			Pose? pose3 = pose;
			if (pose3 == null)
			{
				return poses.RandomItem<Pose>();
			}
			return pose3.GetValueOrDefault();
		}

		public static Pose[] GetPosesForZone(FacilityZone zone)
		{
			Pose[] array;
			if (Scp106PocketExitFinder.PosesForZoneCache.TryGetValue(zone, out array))
			{
				return array;
			}
			Pose[] array2 = SafeLocationFinder.GetLocations((RoomCullingConnection x) => Scp106PocketExitFinder.ValidateConnection(x, zone), (DoorVariant y) => Scp106PocketExitFinder.ValidateDoor(y, zone)).ToArray();
			Scp106PocketExitFinder.PosesForZoneCache[zone] = array2;
			return array2;
		}

		private static bool ValidateDoor(DoorVariant dv, FacilityZone requiredZone)
		{
			BasicDoor basicDoor = dv as BasicDoor;
			if (basicDoor == null)
			{
				return false;
			}
			if (dv.RequiredPermissions.RequiredPermissions != KeycardPermissions.None)
			{
				return false;
			}
			Dictionary<byte, InteractableCollider> dictionary;
			if (!InteractableCollider.AllInstances.TryGetValue(basicDoor, out dictionary))
			{
				return false;
			}
			if (dictionary.Count < 2)
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
			return link.Valid && Scp106PocketExitFinder.ValidateRoom(link.RoomA, requiredZone) && Scp106PocketExitFinder.ValidateRoom(link.RoomB, requiredZone);
		}

		private static bool ValidateRoom(RoomIdentifier room, FacilityZone requiredZone)
		{
			return room.Zone == requiredZone && !Scp106PocketExitFinder.BlacklistedRooms.Contains(room.Name);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			SeedSynchronizer.OnGenerationFinished += Scp106PocketExitFinder.PosesForZoneCache.Clear;
		}

		private const int RequiredTriggers = 2;

		private const int MaxArraySize = 64;

		private const int MaxDistanceSqr = 750;

		private const float ZombieSqrModifier = 0.3f;

		private const float RaycastRange = 11f;

		private const float SurfaceRaycastRange = 45f;

		public static readonly Dictionary<FacilityZone, Pose[]> PosesForZoneCache = new Dictionary<FacilityZone, Pose[]>();

		private static readonly RoomName[] BlacklistedRooms = new RoomName[]
		{
			RoomName.Hcz079,
			RoomName.LczCheckpointA,
			RoomName.LczCheckpointB,
			RoomName.HczCheckpointToEntranceZone,
			RoomName.LczClassDSpawn,
			RoomName.HczTesla
		};

		private static readonly Pose[] PosesNonAlloc = new Pose[64];

		private static readonly Vector3[] PositionsCache = new Vector3[64];

		private static readonly bool[] PositionModifiers = new bool[64];

		private static readonly int Mask = LayerMask.GetMask(new string[] { "Default", "Glass" });
	}
}

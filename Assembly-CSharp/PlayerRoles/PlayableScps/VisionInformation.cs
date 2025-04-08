using System;
using InventorySystem.Items;
using InventorySystem.Items.Usables.Scp244;
using MapGeneration;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps
{
	public readonly struct VisionInformation
	{
		public VisionInformation(ReferenceHub sourceHub, Vector3 targetHub, bool isLooking, bool isOnSameFloor, float lookingAmount, float distance, bool isInLineOfSight, bool isInDarkness, bool isInDistance)
		{
			this.SourceHub = sourceHub;
			this.TargetPosition = targetHub;
			this.IsLooking = isLooking;
			this.LookingAmount = lookingAmount;
			this.Distance = distance;
			this.IsInLineOfSight = isInLineOfSight;
			this.IsInDarkness = isInDarkness;
			this.IsInDistance = isInDistance;
			this.IsOnSameFloor = isOnSameFloor;
		}

		public float LookingAmount { get; }

		public ReferenceHub SourceHub { get; }

		public Vector3 TargetPosition { get; }

		public float Distance { get; }

		public bool IsOnSameFloor { get; }

		public bool IsLooking { get; }

		public bool IsInDistance { get; }

		public bool IsInDarkness { get; }

		public bool IsInLineOfSight { get; }

		public static VisionInformation GetVisionInformation(ReferenceHub source, Transform sourceCam, Vector3 target, float targetRadius = 0f, float visionTriggerDistance = 0f, bool checkFog = true, bool checkLineOfSight = true, int maskLayer = 0, bool checkInDarkness = true)
		{
			bool flag = false;
			bool flag2 = false;
			if (VisionInformation.TargetOnTheSameFloor(sourceCam.position, target))
			{
				flag = true;
				flag2 = true;
			}
			bool flag3 = visionTriggerDistance == 0f;
			Vector3 vector = target - sourceCam.position;
			float magnitude = vector.magnitude;
			if (flag2 && visionTriggerDistance > 0f)
			{
				float num = (checkFog ? ((target.y > 980f) ? visionTriggerDistance : (visionTriggerDistance / 2f)) : visionTriggerDistance);
				if (magnitude <= num)
				{
					flag3 = true;
				}
				flag2 = flag3;
			}
			float num2 = 1f;
			if (flag2)
			{
				flag2 = false;
				if (magnitude < targetRadius)
				{
					if (Vector3.Dot(source.transform.forward, (target - source.transform.position).normalized) > 0f)
					{
						flag2 = true;
						num2 = 1f;
					}
				}
				else
				{
					flag2 = VisionInformation.TargetInViewDirection(source, target, targetRadius, out num2);
				}
			}
			bool flag4 = !checkLineOfSight;
			if (flag2 && checkLineOfSight)
			{
				flag4 = VisionInformation.TargetVisibilityUnobstructed(sourceCam, vector, 0);
				flag2 = flag4;
			}
			bool flag5 = false;
			if (checkInDarkness)
			{
				flag5 = !VisionInformation.CheckAttachments(source) && RoomLightController.IsInDarkenedRoom(target);
				flag2 &= !flag5;
			}
			return new VisionInformation(source, target, flag2, flag, num2, magnitude, flag4, flag5, flag3);
		}

		public static bool TargetVisibilityUnobstructed(Transform sourceCam, Vector3 directionToTarget, int maskLayer = 0)
		{
			if (maskLayer == 0)
			{
				maskLayer = VisionInformation.VisionLayerMask;
			}
			return Physics.RaycastNonAlloc(new Ray(sourceCam.position, directionToTarget.normalized), VisionInformation.RaycastResult, directionToTarget.magnitude, maskLayer) == 0;
		}

		public static bool TargetInViewDirection(ReferenceHub source, Vector3 targetPosition, float targetRadius, out float lookingAmount)
		{
			return VisionInformation.TargetInCustomViewDirection(source.PlayerCameraReference, targetPosition, targetRadius, out lookingAmount, source.aspectRatioSync.XScreenEdge, AspectRatioSync.YScreenEdge);
		}

		public static bool TargetInCustomViewDirection(Transform sourceCam, Vector3 targetPosition, float targetRadius, out float lookingAmount, float xAxisAcceptedEdge, float yAxisAcceptedEdge)
		{
			lookingAmount = 1f;
			if (!Scp244Utils.CheckVisibility(sourceCam.position, targetPosition))
			{
				return false;
			}
			Vector3 vector = sourceCam.InverseTransformPoint(targetPosition);
			if (targetRadius != 0f)
			{
				vector.x = Mathf.MoveTowards(vector.x, 0f, targetRadius);
				vector.y = Mathf.MoveTowards(vector.y, 0f, targetRadius);
			}
			float num = Vector2.Angle(Vector2.up, new Vector2(vector.x, vector.z));
			if (num < xAxisAcceptedEdge)
			{
				float num2 = Vector2.Angle(Vector2.up, new Vector2(vector.y, vector.z));
				if (num2 < yAxisAcceptedEdge)
				{
					lookingAmount = (num + num2) / (xAxisAcceptedEdge + yAxisAcceptedEdge);
					return true;
				}
			}
			return false;
		}

		public static bool TargetOnTheSameFloor(Vector3 firstPosition, Vector3 secondPosition)
		{
			return Mathf.Abs(firstPosition.y - secondPosition.y) < (float)VisionInformation.SameFloorDistanceThreshold;
		}

		private static bool CheckAttachments(ReferenceHub source)
		{
			ItemBase curInstance = source.inventory.CurInstance;
			if (curInstance != null)
			{
				ILightEmittingItem lightEmittingItem = curInstance as ILightEmittingItem;
				if (lightEmittingItem != null)
				{
					return lightEmittingItem.IsEmittingLight;
				}
			}
			return false;
		}

		public VisionInformation.FailReason GetFailReason()
		{
			if (!this.IsOnSameFloor)
			{
				return VisionInformation.FailReason.NotOnSameFloor;
			}
			if (!this.IsInDistance)
			{
				return VisionInformation.FailReason.NotInDistance;
			}
			if (this.LookingAmount >= 1f)
			{
				return VisionInformation.FailReason.NotInView;
			}
			if (!this.IsInLineOfSight)
			{
				return VisionInformation.FailReason.NotInLineOfSight;
			}
			if (this.IsInDarkness)
			{
				return VisionInformation.FailReason.InDarkRoom;
			}
			if (!this.IsLooking)
			{
				return VisionInformation.FailReason.UnkownReason;
			}
			return VisionInformation.FailReason.IsLooking;
		}

		public static bool IsInView(ReferenceHub originHub, ReferenceHub targetHub)
		{
			IFpcRole fpcRole = targetHub.roleManager.CurrentRole as IFpcRole;
			return fpcRole != null && VisionInformation.IsInView(originHub, fpcRole);
		}

		public static bool IsInView(ReferenceHub originHub, IFpcRole fpcRole)
		{
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPosition(fpcRole.FpcModule.Position);
			return VisionInformation.GetVisionInformation(originHub, originHub.PlayerCameraReference, fpcRole.FpcModule.Position, fpcRole.FpcModule.CharacterControllerSettings.Radius, (roomIdentifier != null && roomIdentifier.Zone == FacilityZone.Surface) ? 60f : 30f, false, true, 0, true).IsLooking;
		}

		public const float MaximumVisionDistance = 30f;

		public const float SurfaceMaximumVisionDistance = 60f;

		public static readonly int VisionLayerMask = LayerMask.GetMask(new string[] { "Door", "Default" });

		public static readonly RaycastHit[] RaycastResult = new RaycastHit[1];

		private static readonly int SameFloorDistanceThreshold = 100;

		public enum FailReason
		{
			NotOnSameFloor,
			NotInDistance,
			NotInView,
			NotInLineOfSight,
			InDarkRoom,
			IsLooking,
			UnkownReason
		}
	}
}

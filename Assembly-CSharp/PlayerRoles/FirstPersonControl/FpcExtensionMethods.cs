using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public static class FpcExtensionMethods
	{
		public static Vector3 GetVelocity(this ReferenceHub hub)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return Vector3.zero;
			}
			return fpcRole.FpcModule.Motor.Velocity;
		}

		public static Vector3 GetPosition(this ReferenceHub hub)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return hub.transform.position;
			}
			return fpcRole.FpcModule.Position;
		}

		public static bool IsGrounded(this ReferenceHub hub)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			return fpcRole == null || fpcRole.FpcModule.IsGrounded;
		}

		public static Bounds GenerateTracerBounds(this ReferenceHub hub, float time, bool ignoreTeleports)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return new Bounds(hub.transform.position, Vector3.zero);
			}
			return fpcRole.FpcModule.Tracer.GenerateBounds(time, ignoreTeleports);
		}

		public static bool TryOverridePosition(this ReferenceHub hub, Vector3 position)
		{
			if (!NetworkServer.active)
			{
				return false;
			}
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			fpcRole.FpcModule.ServerOverridePosition(position);
			return true;
		}

		public static bool TryOverrideRotation(this ReferenceHub hub, Vector2 delta)
		{
			if (!NetworkServer.active)
			{
				return false;
			}
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return false;
			}
			fpcRole.FpcModule.ServerOverrideRotation(delta);
			return true;
		}

		public static float GetDot(this IFpcRole target, Transform camera, float radiusMultiplier = 0.5f)
		{
			FirstPersonMovementModule fpcModule = target.FpcModule;
			float num = fpcModule.CharacterControllerSettings.Radius * radiusMultiplier;
			float num2 = 0.5f * fpcModule.CharacterControllerSettings.Height;
			Vector3 vector = camera.InverseTransformPoint(fpcModule.Position);
			vector.x = Mathf.MoveTowards(vector.x, 0f, num);
			vector.y = Mathf.MoveTowards(vector.y, 0f, num2);
			return vector.normalized.z;
		}

		public static float GetDot(this IFpcRole target, float radiusMultiplier = 0.5f)
		{
			return target.GetDot(MainCameraController.CurrentCamera, radiusMultiplier);
		}

		public static float SqrDistanceTo(this IFpcRole target, Vector3 point)
		{
			return (target.FpcModule.Position - point).sqrMagnitude;
		}

		public static float SqrDistanceTo(this IFpcRole target, ReferenceHub other)
		{
			IFpcRole fpcRole = other.roleManager.CurrentRole as IFpcRole;
			return target.SqrDistanceTo((fpcRole != null) ? fpcRole.FpcModule.Position : other.transform.position);
		}

		public static ReferenceHub GetClosest(this IEnumerable<ReferenceHub> players, Vector3 point)
		{
			ReferenceHub referenceHub = null;
			float num = float.MaxValue;
			foreach (ReferenceHub referenceHub2 in players)
			{
				IFpcRole fpcRole = referenceHub2.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					float num2 = fpcRole.SqrDistanceTo(point);
					if (num2 <= num)
					{
						referenceHub = referenceHub2;
						num = num2;
					}
				}
			}
			return referenceHub;
		}

		public static ReferenceHub GetPrimaryTarget(this IEnumerable<ReferenceHub> players, Transform camera)
		{
			Vector3 position = camera.position;
			ReferenceHub referenceHub = null;
			float num = -1f;
			float num2 = float.MaxValue;
			foreach (ReferenceHub referenceHub2 in players)
			{
				IFpcRole fpcRole = referenceHub2.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					float dot = fpcRole.GetDot(camera, 0.5f);
					if (num <= dot)
					{
						float num3 = fpcRole.SqrDistanceTo(position);
						if (num != dot || num3 <= num2)
						{
							num = dot;
							num2 = num3;
							referenceHub = referenceHub2;
						}
					}
				}
			}
			return referenceHub;
		}

		public static void LookAtPoint(this IFpcRole fpc, Vector3 position, float lerp = 1f)
		{
			ICameraController cameraController = fpc as ICameraController;
			Vector3 vector = ((cameraController != null) ? cameraController.CameraPosition : fpc.FpcModule.Position);
			fpc.FpcModule.MouseLook.LookAtDirection((position - vector).normalized, lerp);
		}

		public static void LookAtDirection(this IFpcRole fpc, Vector3 dir, float lerp = 1f)
		{
			fpc.FpcModule.MouseLook.LookAtDirection(dir, lerp);
		}

		public static void LookAtDirection(this FpcMouseLook mouseLook, Vector3 dir, float lerp = 1f)
		{
			Vector2 vector = Quaternion.LookRotation(dir, Vector3.up).eulerAngles;
			mouseLook.CurrentVertical = Mathf.LerpAngle(mouseLook.CurrentVertical, -vector.x, lerp);
			mouseLook.CurrentHorizontal = Mathf.LerpAngle(mouseLook.CurrentHorizontal, vector.y, lerp);
		}
	}
}

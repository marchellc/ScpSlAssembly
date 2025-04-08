using System;
using System.Collections.Generic;
using UnityEngine;

namespace CursorManagement
{
	public static class CursorManager
	{
		public static bool MovementLocked { get; private set; }

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			StaticUnityMethods.OnLateUpdate += CursorManager.LateUpdate;
		}

		private static void LateUpdate()
		{
			CursorOverrideMode cursorOverrideMode;
			bool flag;
			CursorManager.ProcessOverrides(out cursorOverrideMode, out flag);
			if (cursorOverrideMode != CursorOverrideMode.Centered)
			{
				if (cursorOverrideMode == CursorOverrideMode.Confined)
				{
					CursorManager.SetLockMode(CursorLockMode.Confined);
				}
				else
				{
					CursorManager.SetLockMode(CursorLockMode.None);
				}
			}
			else
			{
				CursorManager.SetLockMode(CursorLockMode.Locked);
			}
			CursorManager.MovementLocked = flag;
		}

		private static void ProcessOverrides(out CursorOverrideMode bestOverride, out bool anyMovementLocked)
		{
			bestOverride = CursorOverrideMode.NoOverride;
			anyMovementLocked = false;
			if (!StaticUnityMethods.IsPlaying)
			{
				return;
			}
			int num = 0;
			CursorManager.Overrides.RemoveWhere(delegate(ICursorOverride x)
			{
				if (x != null)
				{
					global::UnityEngine.Object @object = x as global::UnityEngine.Object;
					return @object != null && @object == null;
				}
				return true;
			});
			foreach (ICursorOverride cursorOverride in CursorManager.Overrides)
			{
				num = Mathf.Max(num, (int)cursorOverride.CursorOverride);
				anyMovementLocked |= cursorOverride.LockMovement;
			}
			bestOverride = (CursorOverrideMode)num;
		}

		public static void SetLockMode(CursorLockMode lockMode)
		{
			Cursor.lockState = lockMode;
			Cursor.visible = lockMode != CursorLockMode.Locked;
		}

		public static bool Register(ICursorOverride target)
		{
			return CursorManager.Overrides.Add(target);
		}

		public static bool Unregister(ICursorOverride target)
		{
			return CursorManager.Overrides.Remove(target);
		}

		private static readonly HashSet<ICursorOverride> Overrides = new HashSet<ICursorOverride>();
	}
}

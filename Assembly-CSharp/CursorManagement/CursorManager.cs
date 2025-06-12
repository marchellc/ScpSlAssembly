using System.Collections.Generic;
using UnityEngine;

namespace CursorManagement;

public static class CursorManager
{
	private static readonly HashSet<ICursorOverride> Overrides = new HashSet<ICursorOverride>();

	public static bool MovementLocked { get; private set; }

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		StaticUnityMethods.OnLateUpdate += LateUpdate;
	}

	private static void LateUpdate()
	{
		CursorManager.ProcessOverrides(out var bestOverride, out var anyMovementLocked);
		switch (bestOverride)
		{
		case CursorOverrideMode.Confined:
			CursorManager.SetLockMode(CursorLockMode.Confined);
			break;
		case CursorOverrideMode.Centered:
			CursorManager.SetLockMode(CursorLockMode.Locked);
			break;
		default:
			CursorManager.SetLockMode(CursorLockMode.None);
			break;
		}
		CursorManager.MovementLocked = anyMovementLocked;
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
		CursorManager.Overrides.RemoveWhere((ICursorOverride x) => x == null || (x is Object obj && obj == null));
		foreach (ICursorOverride @override in CursorManager.Overrides)
		{
			num = Mathf.Max(num, (int)@override.CursorOverride);
			anyMovementLocked |= @override.LockMovement;
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
}

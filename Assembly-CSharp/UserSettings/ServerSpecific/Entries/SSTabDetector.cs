using System;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries;

public class SSTabDetector : MonoBehaviour
{
	private static bool _active;

	public static bool IsOpen
	{
		get
		{
			return SSTabDetector._active;
		}
		private set
		{
			if (SSTabDetector._active != value)
			{
				SSTabDetector._active = value;
				SSTabDetector.OnStatusChanged?.Invoke();
			}
		}
	}

	public static event Action OnStatusChanged;

	private void OnEnable()
	{
		SSTabDetector.IsOpen = true;
	}

	private void OnDisable()
	{
		SSTabDetector.IsOpen = false;
	}
}

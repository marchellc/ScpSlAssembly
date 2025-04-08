using System;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSTabDetector : MonoBehaviour
	{
		public static event Action OnStatusChanged;

		public static bool IsOpen
		{
			get
			{
				return SSTabDetector._active;
			}
			private set
			{
				if (SSTabDetector._active == value)
				{
					return;
				}
				SSTabDetector._active = value;
				Action onStatusChanged = SSTabDetector.OnStatusChanged;
				if (onStatusChanged == null)
				{
					return;
				}
				onStatusChanged();
			}
		}

		private void OnEnable()
		{
			SSTabDetector.IsOpen = true;
		}

		private void OnDisable()
		{
			SSTabDetector.IsOpen = false;
		}

		private static bool _active;
	}
}

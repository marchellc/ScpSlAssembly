using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils;

public class ButtonPositionAdjuster : MonoBehaviour
{
	[Serializable]
	private struct SettingsButtonPair
	{
		public Transform Button;

		public Vector3 ButtonRotation;

		public float ForwardOffset;

		public RaycastSettings[] RaycastsByPriority;

		private static readonly CachedLayerMask WallMask = new CachedLayerMask("Default");

		public readonly void DetectAndSetPosition()
		{
			RaycastSettings[] raycastsByPriority = this.RaycastsByPriority;
			for (int i = 0; i < raycastsByPriority.Length; i++)
			{
				RaycastSettings raycastSettings = raycastsByPriority[i];
				Transform raycastOriginDir = raycastSettings.RaycastOriginDir;
				Vector3 position = raycastOriginDir.position;
				Vector3 forward = raycastOriginDir.forward;
				if (Physics.Raycast(position, forward, out var hitInfo, raycastSettings.MaxDistance, SettingsButtonPair.WallMask) && !(Vector3.Dot(forward, -hitInfo.normal) < 0.6f))
				{
					Vector3 position2 = hitInfo.point + hitInfo.normal * this.ForwardOffset;
					Quaternion rotation = Quaternion.LookRotation(hitInfo.normal) * Quaternion.Euler(this.ButtonRotation);
					this.Button.SetPositionAndRotation(position2, rotation);
					break;
				}
			}
		}
	}

	[Serializable]
	private struct RaycastSettings
	{
		public Transform RaycastOriginDir;

		public float MaxDistance;
	}

	private const float MinDot = 0.6f;

	[SerializeField]
	private SettingsButtonPair[] _buttons;

	private void Awake()
	{
		IRoomConnector component = base.GetComponent<IRoomConnector>();
		if (component.RoomsAlreadyRegistered)
		{
			this.SetButtonsPositions();
		}
		else
		{
			component.OnRoomsRegistered += SetButtonsPositions;
		}
	}

	private void SetButtonsPositions()
	{
		SettingsButtonPair[] buttons = this._buttons;
		foreach (SettingsButtonPair settingsButtonPair in buttons)
		{
			settingsButtonPair.DetectAndSetPosition();
		}
	}
}

using System;
using UnityEngine;

namespace PlayerRoles.Spectating;

public class SpectatorTargetTracker : MonoBehaviour
{
	private static bool _trackedTransformSet;

	private static Transform _trackedTransform;

	private static Vector3 _trackedTransformPositionOffset;

	private static Quaternion _trackedTransformRotationOffset;

	private static SpectatableModuleBase _curTracked;

	private static Vector3? _cachedOffsetPos;

	private static Vector3? _cachedOffsetRot;

	private static bool _isSingletonSet;

	private static SpectatorTargetTracker _singleton;

	private static readonly Vector3 DefaultOffsetPos = Vector3.up * 3500f;

	private static readonly Vector3 DefaultOffsetRot = Vector3.zero;

	public static SpectatorTargetTracker Singleton
	{
		get
		{
			return SpectatorTargetTracker._singleton;
		}
		private set
		{
			SpectatorTargetTracker._singleton = value;
			SpectatorTargetTracker._isSingletonSet = value != null;
		}
	}

	private static Transform TrackedTransform
	{
		get
		{
			int num;
			if (SpectatorTargetTracker._trackedTransformSet)
			{
				num = ((SpectatorTargetTracker._trackedTransform == null) ? 1 : 0);
				if (num == 0)
				{
					goto IL_001e;
				}
			}
			else
			{
				num = 1;
			}
			SpectatorTargetTracker._trackedTransformSet = false;
			goto IL_001e;
			IL_001e:
			if (num == 0)
			{
				return SpectatorTargetTracker._trackedTransform;
			}
			return MainCameraController.CurrentCamera;
		}
	}

	[field: SerializeField]
	public GameObject AttachmentsMenu { get; private set; }

	[field: SerializeField]
	public GameObject VoiceChatMenu { get; private set; }

	public static bool TrackerSet { get; private set; }

	public static ReferenceHub LastTrackedPlayer { get; private set; }

	public static SpectatableModuleBase CurrentTarget
	{
		get
		{
			return SpectatorTargetTracker._curTracked;
		}
		set
		{
			if (!(SpectatorTargetTracker._curTracked == value))
			{
				if (SpectatorTargetTracker._curTracked != null)
				{
					SpectatorTargetTracker._curTracked.OnStoppedSpectating();
				}
				SpectatorTargetTracker._curTracked = value;
				MainCameraController.ForceUpdatePosition();
				if (value != null)
				{
					SpectatorTargetTracker.LastTrackedPlayer = (value.MainRole.TryGetOwner(out var hub) ? hub : null);
					value.OnBeganSpectating();
				}
				SpectatorTargetTracker.OnTargetChanged?.Invoke();
			}
		}
	}

	public static Offset CurrentOffset
	{
		get
		{
			if (SpectatorTargetTracker._trackedTransformSet || !SpectatorTargetTracker.TrackerSet)
			{
				Transform trackedTransform = SpectatorTargetTracker.TrackedTransform;
				SpectatorTargetTracker._cachedOffsetPos = trackedTransform.TransformPoint(SpectatorTargetTracker._trackedTransformPositionOffset);
				SpectatorTargetTracker._cachedOffsetRot = (trackedTransform.rotation * SpectatorTargetTracker._trackedTransformRotationOffset).eulerAngles;
			}
			else if (SpectatorTargetTracker.CurrentTarget != null && !SpectatorTargetTracker.CurrentTarget.MainRole.Pooled)
			{
				SpectatorTargetTracker._cachedOffsetPos = SpectatorTargetTracker.CurrentTarget.CameraPosition;
				SpectatorTargetTracker._cachedOffsetRot = SpectatorTargetTracker.CurrentTarget.CameraRotation;
			}
			return new Offset
			{
				position = (SpectatorTargetTracker._cachedOffsetPos ?? SpectatorTargetTracker.DefaultOffsetPos),
				rotation = (SpectatorTargetTracker._cachedOffsetRot ?? SpectatorTargetTracker.DefaultOffsetRot)
			};
		}
	}

	public static event Action OnTargetChanged;

	private void OnEnable()
	{
		SpectatorTargetTracker.TrackerSet = true;
	}

	private void OnDisable()
	{
		SpectatorTargetTracker.CurrentTarget = null;
		SpectatorTargetTracker.TrackerSet = false;
	}

	private void OnDestroy()
	{
		SpectatorTargetTracker.Singleton = null;
		SpectatorTargetTracker._cachedOffsetPos = SpectatorTargetTracker.DefaultOffsetPos;
		SpectatorTargetTracker._cachedOffsetRot = SpectatorTargetTracker.DefaultOffsetRot;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (SpectatorTargetTracker.TrackerSet && prevRole is ISpectatableRole spectatableRole && SpectatorTargetTracker.CurrentTarget == spectatableRole.SpectatorModule)
		{
			SpectatorTargetTracker.CurrentTarget = ((newRole is ISpectatableRole spectatableRole2) ? spectatableRole2.SpectatorModule : null);
		}
		if (!hub.isLocalPlayer)
		{
			return;
		}
		if (newRole is SpectatorRole spectatorRole)
		{
			SpectatorTargetTracker.LastTrackedPlayer = hub;
			if (SpectatorTargetTracker._isSingletonSet)
			{
				SpectatorTargetTracker.Singleton.gameObject.SetActive(value: true);
			}
			else
			{
				SpectatorTargetTracker.Singleton = UnityEngine.Object.Instantiate(spectatorRole.TrackerPrefab, Vector3.zero, Quaternion.identity, null);
			}
			if (newRole is OverwatchRole)
			{
				SpectatorTargetTracker.Singleton.AttachmentsMenu.SetActive(value: false);
				SpectatorTargetTracker.Singleton.VoiceChatMenu.SetActive(value: true);
			}
			else
			{
				SpectatorTargetTracker.Singleton.VoiceChatMenu.SetActive(value: false);
				SpectatorTargetTracker.Singleton.AttachmentsMenu.SetActive(value: true);
			}
		}
		else if (SpectatorTargetTracker.TrackerSet && SpectatorTargetTracker._isSingletonSet)
		{
			SpectatorTargetTracker.Singleton.gameObject.SetActive(value: false);
		}
	}

	public static void SetTrackedTransform(Transform trackedTransform, Vector3 localPosOffset, Quaternion localRotOffset)
	{
		SpectatorTargetTracker._trackedTransform = trackedTransform;
		SpectatorTargetTracker._trackedTransformSet = trackedTransform != null;
		SpectatorTargetTracker._trackedTransformPositionOffset = localPosOffset;
		SpectatorTargetTracker._trackedTransformRotationOffset = localRotOffset;
	}

	public static void SetTrackedTransform(Transform trackedTransform)
	{
		SpectatorTargetTracker.SetTrackedTransform(trackedTransform, Vector3.zero, Quaternion.identity);
	}

	public static bool TryGetTrackedPlayer(out ReferenceHub hub)
	{
		if (SpectatorTargetTracker.TrackerSet && SpectatorTargetTracker.CurrentTarget != null && SpectatorTargetTracker.CurrentTarget.MainRole.TryGetOwner(out hub))
		{
			return true;
		}
		hub = null;
		return false;
	}
}

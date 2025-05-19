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
			return _singleton;
		}
		private set
		{
			_singleton = value;
			_isSingletonSet = value != null;
		}
	}

	private static Transform TrackedTransform
	{
		get
		{
			int num;
			if (_trackedTransformSet)
			{
				num = ((_trackedTransform == null) ? 1 : 0);
				if (num == 0)
				{
					goto IL_001e;
				}
			}
			else
			{
				num = 1;
			}
			_trackedTransformSet = false;
			goto IL_001e;
			IL_001e:
			if (num == 0)
			{
				return _trackedTransform;
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
			return _curTracked;
		}
		set
		{
			if (!(_curTracked == value))
			{
				if (_curTracked != null)
				{
					_curTracked.OnStoppedSpectating();
				}
				_curTracked = value;
				MainCameraController.ForceUpdatePosition();
				if (value != null)
				{
					LastTrackedPlayer = (value.MainRole.TryGetOwner(out var hub) ? hub : null);
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
			if (_trackedTransformSet || !TrackerSet)
			{
				Transform trackedTransform = TrackedTransform;
				_cachedOffsetPos = trackedTransform.TransformPoint(_trackedTransformPositionOffset);
				_cachedOffsetRot = (trackedTransform.rotation * _trackedTransformRotationOffset).eulerAngles;
			}
			else if (CurrentTarget != null && !CurrentTarget.MainRole.Pooled)
			{
				_cachedOffsetPos = CurrentTarget.CameraPosition;
				_cachedOffsetRot = CurrentTarget.CameraRotation;
			}
			Offset result = default(Offset);
			result.position = _cachedOffsetPos ?? DefaultOffsetPos;
			result.rotation = _cachedOffsetRot ?? DefaultOffsetRot;
			return result;
		}
	}

	public static event Action OnTargetChanged;

	private void OnEnable()
	{
		TrackerSet = true;
	}

	private void OnDisable()
	{
		CurrentTarget = null;
		TrackerSet = false;
	}

	private void OnDestroy()
	{
		Singleton = null;
		_cachedOffsetPos = DefaultOffsetPos;
		_cachedOffsetRot = DefaultOffsetRot;
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleManager.OnRoleChanged += OnRoleChanged;
	}

	private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
	{
		if (TrackerSet && prevRole is ISpectatableRole spectatableRole && CurrentTarget == spectatableRole.SpectatorModule)
		{
			CurrentTarget = ((newRole is ISpectatableRole spectatableRole2) ? spectatableRole2.SpectatorModule : null);
		}
		if (!hub.isLocalPlayer)
		{
			return;
		}
		if (newRole is SpectatorRole spectatorRole)
		{
			LastTrackedPlayer = hub;
			if (_isSingletonSet)
			{
				Singleton.gameObject.SetActive(value: true);
			}
			else
			{
				Singleton = UnityEngine.Object.Instantiate(spectatorRole.TrackerPrefab, Vector3.zero, Quaternion.identity, null);
			}
			if (newRole is OverwatchRole)
			{
				Singleton.AttachmentsMenu.SetActive(value: false);
				Singleton.VoiceChatMenu.SetActive(value: true);
			}
			else
			{
				Singleton.VoiceChatMenu.SetActive(value: false);
				Singleton.AttachmentsMenu.SetActive(value: true);
			}
		}
		else if (TrackerSet && _isSingletonSet)
		{
			Singleton.gameObject.SetActive(value: false);
		}
	}

	public static void SetTrackedTransform(Transform trackedTransform, Vector3 localPosOffset, Quaternion localRotOffset)
	{
		_trackedTransform = trackedTransform;
		_trackedTransformSet = trackedTransform != null;
		_trackedTransformPositionOffset = localPosOffset;
		_trackedTransformRotationOffset = localRotOffset;
	}

	public static void SetTrackedTransform(Transform trackedTransform)
	{
		SetTrackedTransform(trackedTransform, Vector3.zero, Quaternion.identity);
	}

	public static bool TryGetTrackedPlayer(out ReferenceHub hub)
	{
		if (TrackerSet && CurrentTarget != null && CurrentTarget.MainRole.TryGetOwner(out hub))
		{
			return true;
		}
		hub = null;
		return false;
	}
}

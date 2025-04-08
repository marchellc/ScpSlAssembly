using System;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public class SpectatorTargetTracker : MonoBehaviour
	{
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
				bool flag = !SpectatorTargetTracker._trackedTransformSet || SpectatorTargetTracker._trackedTransform == null;
				if (flag)
				{
					SpectatorTargetTracker._trackedTransformSet = false;
				}
				if (!flag)
				{
					return SpectatorTargetTracker._trackedTransform;
				}
				return MainCameraController.CurrentCamera;
			}
		}

		public GameObject AttachmentsMenu { get; private set; }

		public GameObject VoiceChatMenu { get; private set; }

		public static event Action OnTargetChanged;

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
				if (SpectatorTargetTracker._curTracked == value)
				{
					return;
				}
				if (SpectatorTargetTracker._curTracked != null)
				{
					SpectatorTargetTracker._curTracked.OnStoppedSpectating();
				}
				SpectatorTargetTracker._curTracked = value;
				MainCameraController.ForceUpdatePosition();
				if (value != null)
				{
					ReferenceHub referenceHub;
					SpectatorTargetTracker.LastTrackedPlayer = (value.MainRole.TryGetOwner(out referenceHub) ? referenceHub : null);
					value.OnBeganSpectating();
				}
				Action onTargetChanged = SpectatorTargetTracker.OnTargetChanged;
				if (onTargetChanged == null)
				{
					return;
				}
				onTargetChanged();
			}
		}

		public static Offset CurrentOffset
		{
			get
			{
				if (SpectatorTargetTracker._trackedTransformSet || !SpectatorTargetTracker.TrackerSet)
				{
					Transform trackedTransform = SpectatorTargetTracker.TrackedTransform;
					SpectatorTargetTracker._cachedOffsetPos = new Vector3?(trackedTransform.TransformPoint(SpectatorTargetTracker._trackedTransformPositionOffset));
					SpectatorTargetTracker._cachedOffsetRot = new Vector3?((trackedTransform.rotation * SpectatorTargetTracker._trackedTransformRotationOffset).eulerAngles);
				}
				else if (SpectatorTargetTracker.CurrentTarget != null && !SpectatorTargetTracker.CurrentTarget.MainRole.Pooled)
				{
					SpectatorTargetTracker._cachedOffsetPos = new Vector3?(SpectatorTargetTracker.CurrentTarget.CameraPosition);
					SpectatorTargetTracker._cachedOffsetRot = new Vector3?(SpectatorTargetTracker.CurrentTarget.CameraRotation);
				}
				return new Offset
				{
					position = (SpectatorTargetTracker._cachedOffsetPos ?? SpectatorTargetTracker.DefaultOffsetPos),
					rotation = (SpectatorTargetTracker._cachedOffsetRot ?? SpectatorTargetTracker.DefaultOffsetRot)
				};
			}
		}

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
			SpectatorTargetTracker._cachedOffsetPos = new Vector3?(SpectatorTargetTracker.DefaultOffsetPos);
			SpectatorTargetTracker._cachedOffsetRot = new Vector3?(SpectatorTargetTracker.DefaultOffsetRot);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			PlayerRoleManager.OnRoleChanged += SpectatorTargetTracker.OnRoleChanged;
		}

		private static void OnRoleChanged(ReferenceHub hub, PlayerRoleBase prevRole, PlayerRoleBase newRole)
		{
			if (SpectatorTargetTracker.TrackerSet)
			{
				ISpectatableRole spectatableRole = prevRole as ISpectatableRole;
				if (spectatableRole != null && SpectatorTargetTracker.CurrentTarget == spectatableRole.SpectatorModule)
				{
					ISpectatableRole spectatableRole2 = newRole as ISpectatableRole;
					SpectatorTargetTracker.CurrentTarget = ((spectatableRole2 != null) ? spectatableRole2.SpectatorModule : null);
				}
			}
			if (!hub.isLocalPlayer)
			{
				return;
			}
			SpectatorRole spectatorRole = newRole as SpectatorRole;
			if (spectatorRole == null)
			{
				if (SpectatorTargetTracker.TrackerSet && SpectatorTargetTracker._isSingletonSet)
				{
					SpectatorTargetTracker.Singleton.gameObject.SetActive(false);
				}
				return;
			}
			SpectatorTargetTracker.LastTrackedPlayer = hub;
			if (SpectatorTargetTracker._isSingletonSet)
			{
				SpectatorTargetTracker.Singleton.gameObject.SetActive(true);
			}
			else
			{
				SpectatorTargetTracker.Singleton = global::UnityEngine.Object.Instantiate<SpectatorTargetTracker>(spectatorRole.TrackerPrefab, Vector3.zero, Quaternion.identity, null);
			}
			if (newRole is OverwatchRole)
			{
				SpectatorTargetTracker.Singleton.AttachmentsMenu.SetActive(false);
				SpectatorTargetTracker.Singleton.VoiceChatMenu.SetActive(true);
				return;
			}
			SpectatorTargetTracker.Singleton.VoiceChatMenu.SetActive(false);
			SpectatorTargetTracker.Singleton.AttachmentsMenu.SetActive(true);
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
	}
}

using System.Collections.Generic;
using CustomPlayerEffects;
using MapGeneration;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.PlayableScps;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp1344;

public class Scp1344HumanXrayProvider : Scp1344XrayProviderBase
{
	private class OrbInstance
	{
		public readonly ReferenceHub Target;

		private readonly Scp1344Orb _orbInstance;

		private readonly Color _invisiblePlayerColor;

		private readonly Scp1344Detected _detectedFx;

		private readonly Invisible _invisibleFx;

		private readonly Scp1344HumanXrayProvider _parent;

		private bool _orbDestroyed;

		private long _prevCycleIndex;

		private Color _lastColor;

		public bool Tracked { get; private set; }

		public OrbInstance(ReferenceHub player, Scp1344HumanXrayProvider parent)
		{
			Target = player;
			_parent = parent;
			_orbInstance = Object.Instantiate(parent._orbTemplate);
			_invisiblePlayerColor = parent._invisiblePlayerColor;
			_detectedFx = player.playerEffectsController.GetEffect<Scp1344Detected>();
			_invisibleFx = player.playerEffectsController.GetEffect<Invisible>();
			_orbInstance.OnDestroyCallback += delegate
			{
				_orbDestroyed = true;
			};
		}

		public void DestroySelf()
		{
			if (!_orbDestroyed)
			{
				Object.Destroy(_orbInstance.gameObject);
				_orbDestroyed = true;
			}
		}

		public void UpdateTracking(ReferenceHub ownerHub, float normalizedHealth)
		{
			if (_orbDestroyed)
			{
				return;
			}
			Tracked = TryUpdate(ownerHub, normalizedHealth, out _lastColor, out var position, out var scale);
			double num = NetworkTime.time * 0.20000000298023224;
			long num2 = (long)num;
			float num3 = (float)(num - (double)num2) / 0.2f;
			if (Tracked && ownerHub.IsPOV)
			{
				if (_prevCycleIndex != num2 || num3 < 1.1f)
				{
					_orbInstance.Position = position;
					_orbInstance.Scale = scale;
				}
				float num4 = Mathf.InverseLerp(1.5f, 0.5f, num3);
				_orbInstance.ParticleColor = _lastColor * new Color(1f, 1f, 1f, num4);
				_orbInstance.ParticleEmissionEnabled = num4 > 0f;
			}
			else
			{
				_orbInstance.ParticleEmissionEnabled = false;
			}
			if (Tracked && NetworkServer.active)
			{
				_detectedFx.ServerRegisterObserver(_parent);
			}
			_prevCycleIndex = num2;
		}

		public void UpdatePositionPostProcessing()
		{
			if (!NetworkServer.active && _invisibleFx.IsEnabled && Target.roleManager.CurrentRole is FpcStandardRoleBase && !Target.IsPOV)
			{
				Target.transform.position = FpcMotor.InvisiblePosition;
			}
		}

		private bool TryUpdate(ReferenceHub owner, float normalizedHealth, out Color color, out Vector3 position, out Vector3 scale)
		{
			color = default(Color);
			position = default(Vector3);
			scale = default(Vector3);
			if ((object)Target == owner)
			{
				return false;
			}
			if (!(Target.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
			{
				return false;
			}
			position = fpcStandardRoleBase.FpcModule.Position;
			float num = ((Target.GetLastKnownZone() == FacilityZone.Surface) ? 36f : 18f);
			if (!IsInVision(owner, fpcStandardRoleBase, position, num + 5f, out var inSight, out var targetDistance))
			{
				return false;
			}
			position = GetParticlePosition(fpcStandardRoleBase, position);
			scale = GetParticleScale(owner.GetTeam(), fpcStandardRoleBase) * Vector3.one;
			color = Mathf.Clamp(normalizedHealth, 0.2f, 1f) * GetParticleColor(Target, fpcStandardRoleBase, _invisiblePlayerColor);
			if (!inSight && num < targetDistance)
			{
				color.a = Mathf.Clamp01((num + 5f - targetDistance) / 5f);
			}
			return true;
		}
	}

	private const float VisionDistance = 18f;

	private const float SurfaceVisionDistance = 36f;

	private const float VisionTranslucentDistance = 5f;

	private const float EnemyParticleScale = 2f;

	private const float AllyParticleScale = 1f;

	private const float ScpParticleScale = 2.5f;

	private const float MinHealthScaleMultiplier = 0.2f;

	private const float FallbackHeadOffset = 0.7f;

	private const float OrbUpdateCycleFrequency = 0.2f;

	private const float OrbStartFade = 0.5f;

	private const float OrbTrackingDuration = 1.1f;

	private const float OrbFadeDuration = 1f;

	private readonly List<OrbInstance> _orbInstances = new List<OrbInstance>();

	[SerializeField]
	private Scp1344Orb _orbTemplate;

	[SerializeField]
	private Color _invisiblePlayerColor;

	private bool _eventsSet;

	public bool GetVisibilityForTarget(ReferenceHub target)
	{
		foreach (OrbInstance orbInstance in _orbInstances)
		{
			if ((object)target == orbInstance.Target)
			{
				return orbInstance.Tracked;
			}
		}
		return false;
	}

	public override void OnVisionEnabled()
	{
		base.OnVisionEnabled();
		foreach (ReferenceHub allHub in ReferenceHub.AllHubs)
		{
			_orbInstances.Add(new OrbInstance(allHub, this));
		}
		SetEvents(newSet: true);
	}

	public override void OnVisionDisabled()
	{
		base.OnVisionDisabled();
		SetEvents(newSet: false);
		foreach (OrbInstance orbInstance in _orbInstances)
		{
			orbInstance.DestroySelf();
		}
		_orbInstances.Clear();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		float normalizedValue = base.Hub.playerStats.GetModule<HealthStat>().NormalizedValue;
		foreach (OrbInstance orbInstance in _orbInstances)
		{
			orbInstance.UpdateTracking(base.Hub, normalizedValue);
		}
	}

	private void OnDestroy()
	{
		SetEvents(newSet: false);
	}

	private void SetEvents(bool newSet)
	{
		if (_eventsSet != newSet)
		{
			if (newSet)
			{
				_eventsSet = true;
				ReferenceHub.OnPlayerAdded += OnPlayerAdded;
				ReferenceHub.OnPlayerRemoved += OnPlayerRemoved;
				FirstPersonMovementModule.OnPositionUpdated += OnPositionsSet;
			}
			else
			{
				_eventsSet = false;
				ReferenceHub.OnPlayerAdded -= OnPlayerAdded;
				ReferenceHub.OnPlayerRemoved -= OnPlayerRemoved;
				FirstPersonMovementModule.OnPositionUpdated -= OnPositionsSet;
			}
		}
	}

	private void OnPlayerRemoved(ReferenceHub hub)
	{
		for (int num = _orbInstances.Count - 1; num >= 0; num--)
		{
			if ((object)hub == _orbInstances[num].Target)
			{
				_orbInstances.RemoveAt(num);
			}
		}
	}

	private void OnPlayerAdded(ReferenceHub hub)
	{
		_orbInstances.Add(new OrbInstance(hub, this));
	}

	private void OnPositionsSet()
	{
		_orbInstances.ForEach(delegate(OrbInstance x)
		{
			x.UpdatePositionPostProcessing();
		});
	}

	private static bool IsInVision(ReferenceHub owner, FpcStandardRoleBase fpcTarget, Vector3 targetPosition, float maxDistance, out bool inSight, out float targetDistance)
	{
		float radius = fpcTarget.FpcModule.CharacterControllerSettings.Radius;
		VisionInformation visionInformation = VisionInformation.GetVisionInformation(owner, owner.PlayerCameraReference, targetPosition, radius, 0f, checkFog: false);
		targetDistance = visionInformation.Distance;
		inSight = visionInformation.IsLooking;
		return targetDistance < maxDistance;
	}

	private static Color GetParticleColor(ReferenceHub hub, FpcStandardRoleBase fpc, Color invisiblePlayerColor)
	{
		if (hub.playerEffectsController.TryGetEffect<Invisible>(out var playerEffect) && playerEffect.IsEnabled)
		{
			return invisiblePlayerColor;
		}
		if (fpc is ICustomNicknameDisplayRole customNicknameDisplayRole)
		{
			return customNicknameDisplayRole.NicknameColor;
		}
		return fpc.RoleColor;
	}

	private static float GetParticleScale(Team ownerTeam, FpcStandardRoleBase targetRole)
	{
		if (targetRole.Team == Team.SCPs && targetRole.RoleTypeId != RoleTypeId.Scp0492)
		{
			return 2.5f;
		}
		if (ownerTeam == targetRole.Team)
		{
			return 1f;
		}
		return 2f;
	}

	private static Vector3 GetParticlePosition(FpcStandardRoleBase role, Vector3 originalPosition)
	{
		Vector3 vector = originalPosition + new Vector3(0f, 0.7f, 0f);
		if (!(role.FpcModule.CharacterModelInstance is AnimatedCharacterModel { Animator: var animator }))
		{
			return vector;
		}
		Avatar avatar = animator.avatar;
		if (avatar != null && avatar.isHuman)
		{
			Transform boneTransform = animator.GetBoneTransform(HumanBodyBones.Head);
			if (boneTransform == null)
			{
				return vector;
			}
			Vector3 position = boneTransform.position;
			float sqrMagnitude = (vector - position).sqrMagnitude;
			if (sqrMagnitude > 100f)
			{
				return vector;
			}
			vector = Vector3.Lerp(vector, position, sqrMagnitude);
		}
		return vector;
	}
}

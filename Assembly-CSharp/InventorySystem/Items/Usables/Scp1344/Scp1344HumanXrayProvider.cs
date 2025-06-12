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
			this.Target = player;
			this._parent = parent;
			this._orbInstance = Object.Instantiate(parent._orbTemplate);
			this._invisiblePlayerColor = parent._invisiblePlayerColor;
			this._detectedFx = player.playerEffectsController.GetEffect<Scp1344Detected>();
			this._invisibleFx = player.playerEffectsController.GetEffect<Invisible>();
			this._orbInstance.OnDestroyCallback += delegate
			{
				this._orbDestroyed = true;
			};
		}

		public void DestroySelf()
		{
			if (!this._orbDestroyed)
			{
				Object.Destroy(this._orbInstance.gameObject);
				this._orbDestroyed = true;
			}
		}

		public void UpdateTracking(ReferenceHub ownerHub, float normalizedHealth)
		{
			if (this._orbDestroyed)
			{
				return;
			}
			this.Tracked = this.TryUpdate(ownerHub, normalizedHealth, out this._lastColor, out var position, out var scale);
			double num = NetworkTime.time * 0.20000000298023224;
			long num2 = (long)num;
			float num3 = (float)(num - (double)num2) / 0.2f;
			if (this.Tracked && ownerHub.IsPOV)
			{
				if (this._prevCycleIndex != num2 || num3 < 1.1f)
				{
					this._orbInstance.Position = position;
					this._orbInstance.Scale = scale;
				}
				float num4 = Mathf.InverseLerp(1.5f, 0.5f, num3);
				this._orbInstance.ParticleColor = this._lastColor * new Color(1f, 1f, 1f, num4);
				this._orbInstance.ParticleEmissionEnabled = num4 > 0f;
			}
			else
			{
				this._orbInstance.ParticleEmissionEnabled = false;
			}
			if (this.Tracked && NetworkServer.active)
			{
				this._detectedFx.ServerRegisterObserver(this._parent);
			}
			this._prevCycleIndex = num2;
		}

		public void UpdatePositionPostProcessing()
		{
			if (!NetworkServer.active && this._invisibleFx.IsEnabled && this.Target.roleManager.CurrentRole is FpcStandardRoleBase && !this.Target.IsPOV)
			{
				this.Target.transform.position = FpcMotor.InvisiblePosition;
			}
		}

		private bool TryUpdate(ReferenceHub owner, float normalizedHealth, out Color color, out Vector3 position, out Vector3 scale)
		{
			color = default(Color);
			position = default(Vector3);
			scale = default(Vector3);
			if ((object)this.Target == owner)
			{
				return false;
			}
			if (!(this.Target.roleManager.CurrentRole is FpcStandardRoleBase fpcStandardRoleBase))
			{
				return false;
			}
			position = fpcStandardRoleBase.FpcModule.Position;
			float num = ((this.Target.GetLastKnownZone() == FacilityZone.Surface) ? 36f : 18f);
			if (!Scp1344HumanXrayProvider.IsInVision(owner, fpcStandardRoleBase, position, num + 5f, out var inSight, out var targetDistance))
			{
				return false;
			}
			position = Scp1344HumanXrayProvider.GetParticlePosition(fpcStandardRoleBase, position);
			scale = Scp1344HumanXrayProvider.GetParticleScale(owner.GetTeam(), fpcStandardRoleBase) * Vector3.one;
			color = Mathf.Clamp(normalizedHealth, 0.2f, 1f) * Scp1344HumanXrayProvider.GetParticleColor(this.Target, fpcStandardRoleBase, this._invisiblePlayerColor);
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
		foreach (OrbInstance orbInstance in this._orbInstances)
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
			this._orbInstances.Add(new OrbInstance(allHub, this));
		}
		this.SetEvents(newSet: true);
	}

	public override void OnVisionDisabled()
	{
		base.OnVisionDisabled();
		this.SetEvents(newSet: false);
		foreach (OrbInstance orbInstance in this._orbInstances)
		{
			orbInstance.DestroySelf();
		}
		this._orbInstances.Clear();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();
		float normalizedValue = base.Hub.playerStats.GetModule<HealthStat>().NormalizedValue;
		foreach (OrbInstance orbInstance in this._orbInstances)
		{
			orbInstance.UpdateTracking(base.Hub, normalizedValue);
		}
	}

	private void OnDestroy()
	{
		this.SetEvents(newSet: false);
	}

	private void SetEvents(bool newSet)
	{
		if (this._eventsSet != newSet)
		{
			if (newSet)
			{
				this._eventsSet = true;
				ReferenceHub.OnPlayerAdded += OnPlayerAdded;
				ReferenceHub.OnPlayerRemoved += OnPlayerRemoved;
				FirstPersonMovementModule.OnPositionUpdated += OnPositionsSet;
			}
			else
			{
				this._eventsSet = false;
				ReferenceHub.OnPlayerAdded -= OnPlayerAdded;
				ReferenceHub.OnPlayerRemoved -= OnPlayerRemoved;
				FirstPersonMovementModule.OnPositionUpdated -= OnPositionsSet;
			}
		}
	}

	private void OnPlayerRemoved(ReferenceHub hub)
	{
		for (int num = this._orbInstances.Count - 1; num >= 0; num--)
		{
			if ((object)hub == this._orbInstances[num].Target)
			{
				this._orbInstances.RemoveAt(num);
			}
		}
	}

	private void OnPlayerAdded(ReferenceHub hub)
	{
		this._orbInstances.Add(new OrbInstance(hub, this));
	}

	private void OnPositionsSet()
	{
		this._orbInstances.ForEach(delegate(OrbInstance x)
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

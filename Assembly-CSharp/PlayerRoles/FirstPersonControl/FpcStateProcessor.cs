using System;
using System.Diagnostics;
using GameCore;
using Mirror;
using PlayerStatsSystem;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace PlayerRoles.FirstPersonControl;

public class FpcStateProcessor
{
	internal const float DefaultRampupTime = 3.11f;

	private bool _firstUpdate;

	private readonly StaminaStat _stat;

	private readonly FirstPersonMovementModule _mod;

	private readonly AnimationCurve _regenerationOverTime;

	private readonly float _useRate;

	private readonly float _respawnImmunity;

	private readonly Stopwatch _regenStopwatch;

	private readonly Transform _camPivot;

	private const float MinValueToStartSprint = 0.05f;

	private const float EyeHeight = 0.088f;

	private static int _layerMask;

	private static readonly ToggleOrHoldInput SprintInput = new ToggleOrHoldInput(ActionName.Run, new CachedUserSetting<bool>(MiscControlsSetting.SprintToggle));

	private static readonly ToggleOrHoldInput SneakInput = new ToggleOrHoldInput(ActionName.Sneak, new CachedUserSetting<bool>(MiscControlsSetting.SneakToggle));

	internal static float DefaultUseRate => ConfigFile.ServerConfig.GetFloat("stamina_balance_use", 0.05f);

	internal static float DefaultSpawnImmunity => ConfigFile.ServerConfig.GetFloat("stamina_balance_immunity", 3f);

	internal static float DefaultRegenCooldown => ConfigFile.ServerConfig.GetFloat("stamina_balance_regen_cd", 1f);

	internal static float DefaultRegenSpeed => 0.126f * ConfigFile.ServerConfig.GetFloat("stamina_balance_regen_speed", 1f);

	public float CrouchPercent { get; private set; }

	protected ReferenceHub Hub { get; private set; }

	public static LayerMask Mask
	{
		get
		{
			if (FpcStateProcessor._layerMask == 0)
			{
				int layer = LayerMask.NameToLayer("Player");
				for (int i = 0; i < 32; i++)
				{
					if (!Physics.GetIgnoreLayerCollision(layer, i))
					{
						FpcStateProcessor._layerMask |= 1 << i;
					}
				}
			}
			return FpcStateProcessor._layerMask;
		}
	}

	protected virtual float ServerUseRate
	{
		get
		{
			if (this.Hub.roleManager.CurrentRole.ActiveTime <= this._respawnImmunity)
			{
				return 0f;
			}
			float num = this._useRate * this.Hub.inventory.StaminaUsageMultiplier;
			for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
			{
				if (this.Hub.playerEffectsController.AllEffects[i] is IStaminaModifier { StaminaModifierActive: not false } staminaModifier)
				{
					num *= staminaModifier.StaminaUsageMultiplier;
				}
			}
			return num;
		}
	}

	protected virtual float ServerRegenRate
	{
		get
		{
			float num = this._regenerationOverTime.Evaluate((float)this._regenStopwatch.Elapsed.TotalSeconds);
			for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
			{
				if (this.Hub.playerEffectsController.AllEffects[i] is IStaminaModifier { StaminaModifierActive: not false } staminaModifier)
				{
					num *= staminaModifier.StaminaRegenMultiplier;
				}
			}
			return num;
		}
	}

	protected virtual bool SprintingDisabled
	{
		get
		{
			for (int i = 0; i < this.Hub.playerEffectsController.EffectsLength; i++)
			{
				if (this.Hub.playerEffectsController.AllEffects[i] is IStaminaModifier { StaminaModifierActive: not false, SprintingDisabled: not false })
				{
					return true;
				}
			}
			return this.Hub.inventory.SprintingDisabled;
		}
	}

	public FpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module)
		: this(hub, module, hub.IsHuman() ? FpcStateProcessor.DefaultUseRate : 0f, FpcStateProcessor.DefaultSpawnImmunity, FpcStateProcessor.DefaultRegenCooldown, FpcStateProcessor.DefaultRegenSpeed, 3.11f)
	{
	}

	public FpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module, float useRate, float spawnImmunity, float regenCooldown, float regenSpeed, float rampupTime)
		: this(hub, module, useRate, spawnImmunity, new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f, 0f, 0f), new Keyframe(regenCooldown, 0f, 0f, 0f, 0f, 0f), new Keyframe(rampupTime + regenCooldown, regenSpeed, 0.00926f, 0.00926f, 0.1068f, 0.3f)))
	{
	}

	public FpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module, float useRate, float spawnImmunity, AnimationCurve regenCurve)
	{
		this.Hub = hub;
		this._mod = module;
		this._camPivot = this.Hub.PlayerCameraReference.parent;
		this._stat = this.Hub.playerStats.GetModule<StaminaStat>();
		this._firstUpdate = NetworkServer.active || this.Hub.isLocalPlayer;
		this.CrouchPercent = 0f;
		this._useRate = useRate;
		this._respawnImmunity = spawnImmunity;
		this._regenerationOverTime = regenCurve;
		if (NetworkServer.active)
		{
			this._regenStopwatch = Stopwatch.StartNew();
		}
	}

	public virtual void ClientUpdateInput(FirstPersonMovementModule moduleRef, float walkSpeed, out PlayerMovementState valueToSend)
	{
		if (FpcStateProcessor.SprintInput.KeyDown)
		{
			FpcStateProcessor.SneakInput.ResetToggle();
		}
		if (FpcStateProcessor.SneakInput.KeyDown)
		{
			FpcStateProcessor.SprintInput.ResetToggle();
		}
		bool flag = moduleRef.CurrentMovementState == PlayerMovementState.Sprinting;
		bool flag2 = false;
		if (FpcStateProcessor.SneakInput.IsActive || flag2)
		{
			valueToSend = ((!flag2) ? PlayerMovementState.Sneaking : PlayerMovementState.Crouching);
			moduleRef.CurrentMovementState = valueToSend;
			return;
		}
		if (!FpcStateProcessor.SprintInput.IsActive)
		{
			valueToSend = PlayerMovementState.Walking;
			moduleRef.CurrentMovementState = valueToSend;
			return;
		}
		bool flag3 = this._stat.CurValue > 0f;
		if (flag3 && !this.SprintingDisabled && (flag || this._stat.CurValue > 0.05f))
		{
			bool flag4 = this._mod.Motor.Velocity.SqrMagnitudeIgnoreY() < walkSpeed * walkSpeed;
			valueToSend = (flag4 ? PlayerMovementState.Walking : PlayerMovementState.Sprinting);
			moduleRef.CurrentMovementState = PlayerMovementState.Sprinting;
			return;
		}
		if (!flag3)
		{
			FpcStateProcessor.SprintInput.ResetAll();
		}
		valueToSend = PlayerMovementState.Walking;
		moduleRef.CurrentMovementState = valueToSend;
	}

	public virtual PlayerMovementState UpdateMovementState(PlayerMovementState state)
	{
		bool isCrouching = state == PlayerMovementState.Crouching;
		float height = this._mod.CharacterControllerSettings.Height;
		float num = height * this._mod.CrouchHeightRatio;
		if (this.UpdateCrouching(isCrouching, num, height) || this._firstUpdate)
		{
			this._firstUpdate = false;
			float num2 = Mathf.Lerp(0f, (height - num) / 2f, this.CrouchPercent);
			float num3 = Mathf.Lerp(height, num, this.CrouchPercent);
			float radius = this._mod.CharController.radius;
			this._mod.CharController.height = num3;
			this._mod.CharController.center = Vector3.down * num2;
			this._camPivot.localPosition = Vector3.up * (num3 / 2f - num2 - radius + 0.088f);
		}
		if (!NetworkServer.active || this._useRate == 0f)
		{
			return state;
		}
		if (state == PlayerMovementState.Sprinting)
		{
			if (this._stat.CurValue > 0f && !this.SprintingDisabled)
			{
				float value = this._stat.CurValue - Time.deltaTime * this.ServerUseRate;
				this._stat.CurValue = Mathf.Clamp01(value);
				this._regenStopwatch.Restart();
				return PlayerMovementState.Sprinting;
			}
			state = PlayerMovementState.Walking;
		}
		if (this._stat.CurValue >= 1f)
		{
			return state;
		}
		this._stat.CurValue = Mathf.Clamp01(this._stat.CurValue + this.ServerRegenRate * Time.deltaTime);
		return state;
	}

	private bool UpdateCrouching(bool isCrouching, float cH, float nH)
	{
		if (this.CrouchPercent <= 0f && !isCrouching)
		{
			return false;
		}
		if (isCrouching && cH < nH && this._mod.CrouchSpeed != 0f)
		{
			this.CrouchPercent = this.IncreasedCrouch();
		}
		else
		{
			float maxHeight = this.GetMaxHeight(this.Hub.transform.position, cH, nH);
			this.CrouchPercent = Mathf.Max(this.DecreasedCrouch(), Mathf.InverseLerp(nH, cH, maxHeight));
		}
		if (!NetworkServer.active)
		{
			return this.Hub.isLocalPlayer;
		}
		return true;
	}

	private float DecreasedCrouch()
	{
		float t = Mathf.Abs(this.CrouchPercent - 0.5f) * 2f;
		float num = Mathf.Lerp(5f, 0.4f, t);
		return Math.Max(0f, this.CrouchPercent - Time.deltaTime * num);
	}

	private float IncreasedCrouch()
	{
		float num = Mathf.SmoothStep(4.5f, 0.8f, this.CrouchPercent);
		return Mathf.Min(1f, this.CrouchPercent + Time.deltaTime * num);
	}

	private float GetMaxHeight(Vector3 pos, float cH, float nH)
	{
		float radius = this._mod.CharacterControllerSettings.Radius;
		pos.y -= nH / 2f - radius;
		if (!Physics.SphereCast(pos, radius, Vector3.up, out var hitInfo, nH, FpcStateProcessor.Mask))
		{
			return nH;
		}
		return hitInfo.distance + radius;
	}
}

using System.Collections.Generic;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.MarshmallowMan;

public class MarshmallowEffect : StatusEffectBase, IDamageModifierEffect, IMovementSpeedModifier, IStaminaModifier, IFriendlyFireModifier
{
	private static readonly int DeployAnimHash = Shader.PropertyToID("_DeployStatus");

	private static readonly int FadeHash = Shader.PropertyToID("_Fade");

	private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");

	private static readonly int AttackMirrorHash = Animator.StringToHash("AttackMirror");

	[SerializeField]
	private AnimatedCharacterModel _marshmallowModelTemplate;

	[SerializeField]
	private float _deployTime;

	[SerializeField]
	private float _turnBackTime;

	[SerializeField]
	private AnimationCurve _originalFadeOverProgress;

	private bool _turningBack;

	private bool _mirrorAttack;

	private float _progress;

	private bool _instanceSet;

	private Transform _parent;

	private AnimatedCharacterModel _marshmallowModelInstance;

	private AnimatedCharacterModel _originalCharacterModel;

	private MarshmallowAudio _marshmallowAudio;

	private const float DamageReduction = 0.25f;

	private const float TurnBackTime = 1.1f;

	private const byte TurnBackIntensityCode = byte.MaxValue;

	private float DeployProgress
	{
		get
		{
			return _progress;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			if (num != _progress)
			{
				_progress = num;
				UpdateMaterials();
			}
		}
	}

	public bool DamageModifierActive => base.IsEnabled;

	public bool MovementModifierActive => base.IsEnabled;

	public float MovementSpeedMultiplier => 1.55f;

	public float MovementSpeedLimit
	{
		get
		{
			if (base.Intensity != byte.MaxValue || !base.IsLocalPlayer)
			{
				return float.MaxValue;
			}
			return 0.5f;
		}
	}

	public bool StaminaModifierActive => base.IsEnabled;

	public bool SprintingDisabled => true;

	public float GetDamageModifier(float baseDamage, DamageHandlerBase handler, HitboxType hitboxType)
	{
		return 0.75f;
	}

	public bool AllowFriendlyFire(float baseDamage, AttackerDamageHandler handler, HitboxType hitboxType)
	{
		return true;
	}

	protected override void Enabled()
	{
		base.Enabled();
		SetupLink();
		MarshmallowItem.OnSwing += OnSwing;
		MarshmallowItem.OnHolsterRequested += OnHolsterRequested;
		if (!NetworkServer.active)
		{
			return;
		}
		base.Hub.inventory.ServerDropEverything();
		foreach (KeyValuePair<ItemType, ItemBase> availableItem in InventoryItemLoader.AvailableItems)
		{
			if (availableItem.Value is MarshmallowItem)
			{
				base.Hub.inventory.ServerAddItem(availableItem.Key, ItemAddReason.StatusEffect, 0);
				break;
			}
		}
	}

	protected override void Disabled()
	{
		base.Disabled();
		MarshmallowItem.OnSwing -= OnSwing;
		MarshmallowItem.OnHolsterRequested -= OnHolsterRequested;
		Unlink();
		_progress = 0f;
		_turningBack = false;
		_mirrorAttack = false;
		if (NetworkServer.active)
		{
			Inventory inventory = base.Hub.inventory;
			if (inventory.CurInstance is MarshmallowItem)
			{
				inventory.ServerDropItem(inventory.CurItem.SerialNumber);
			}
		}
	}

	protected override void OnEffectUpdate()
	{
		base.OnEffectUpdate();
		if (_instanceSet)
		{
			_marshmallowAudio.Setup(base.Hub.inventory.CurItem.SerialNumber, _parent);
		}
		else
		{
			SetupLink();
		}
		if (_turningBack)
		{
			DeployProgress -= Time.deltaTime / _turnBackTime;
		}
		else
		{
			DeployProgress += Time.deltaTime / _deployTime;
		}
		if (NetworkServer.active && base.Duration != 0f && !(base.TimeLeft > 1.1f) && base.IsEnabled)
		{
			base.Intensity = byte.MaxValue;
		}
	}

	protected override void IntensityChanged(byte prevState, byte newState)
	{
		base.IntensityChanged(prevState, newState);
		if (NetworkServer.active && newState == byte.MaxValue && prevState != 0 && base.Hub.inventory.CurInstance is MarshmallowItem marshmallowItem && !(marshmallowItem == null))
		{
			marshmallowItem.ServerRequestHolster();
		}
	}

	private void OnDestroy()
	{
		Unlink();
	}

	private void SetupLink()
	{
		if (base.Hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule.CharacterModelInstance is AnimatedCharacterModel originalCharacterModel)
		{
			_instanceSet = true;
			_originalCharacterModel = originalCharacterModel;
			_parent = base.Hub.transform;
			_marshmallowModelInstance = Object.Instantiate(_marshmallowModelTemplate, _parent);
			_marshmallowAudio = _marshmallowModelInstance.GetComponent<MarshmallowAudio>();
			_marshmallowModelInstance.Pooled = false;
			_marshmallowModelInstance.Setup(base.Hub, fpcRole, _marshmallowModelTemplate.transform.position, _marshmallowModelTemplate.transform.rotation);
			_originalCharacterModel.OnVisibilityChanged += OnVisibilityChanged;
			_originalCharacterModel.OnFadeChanged += OnFadeChanged;
			UpdateMaterials();
			OnVisibilityChanged();
		}
	}

	private void Unlink()
	{
		if (_instanceSet)
		{
			_originalCharacterModel.OnVisibilityChanged -= OnVisibilityChanged;
			_originalCharacterModel.OnFadeChanged -= OnFadeChanged;
			SetShaderFloat(_originalCharacterModel, FadeHash, _originalCharacterModel.Fade);
			_marshmallowModelInstance.Pooled = true;
			_marshmallowModelInstance.ResetObject();
			Object.Destroy(_marshmallowModelInstance.gameObject);
			_instanceSet = false;
		}
	}

	private void UpdateMaterials()
	{
		if (_instanceSet)
		{
			float deployProgress = DeployProgress;
			SetShaderFloat(_marshmallowModelInstance, DeployAnimHash, deployProgress);
			SetShaderFloat(_originalCharacterModel, FadeHash, _originalFadeOverProgress.Evaluate(deployProgress));
		}
	}

	private void SetShaderFloat(AnimatedCharacterModel model, int hash, float fade)
	{
		model.FadeableMaterials.ForEach(delegate(Material x)
		{
			x.SetFloat(hash, fade);
		});
	}

	private void OnHolsterRequested(ushort serial)
	{
		if (serial == base.Hub.inventory.CurItem.SerialNumber)
		{
			_turningBack = true;
		}
	}

	private void OnSwing(ushort serial)
	{
		if (serial == base.Hub.inventory.CurItem.SerialNumber)
		{
			_marshmallowModelInstance.Animator.SetBool(AttackMirrorHash, _mirrorAttack);
			_marshmallowModelInstance.Animator.SetTrigger(AttackTriggerHash);
			_mirrorAttack = !_mirrorAttack;
		}
	}

	private void OnVisibilityChanged()
	{
		if (_instanceSet)
		{
			_marshmallowModelInstance.SetVisibility(_originalCharacterModel.IsVisible);
		}
	}

	private void OnFadeChanged()
	{
		if (_instanceSet)
		{
			_marshmallowModelInstance.Fade = _originalCharacterModel.Fade;
			UpdateMaterials();
		}
	}
}

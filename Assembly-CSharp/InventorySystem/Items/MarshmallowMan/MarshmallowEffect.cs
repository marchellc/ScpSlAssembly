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
			return this._progress;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			if (num != this._progress)
			{
				this._progress = num;
				this.UpdateMaterials();
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
		this.SetupLink();
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
		this.Unlink();
		this._progress = 0f;
		this._turningBack = false;
		this._mirrorAttack = false;
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
		if (this._instanceSet)
		{
			this._marshmallowAudio.Setup(base.Hub.inventory.CurItem.SerialNumber, this._parent);
		}
		else
		{
			this.SetupLink();
		}
		if (this._turningBack)
		{
			this.DeployProgress -= Time.deltaTime / this._turnBackTime;
		}
		else
		{
			this.DeployProgress += Time.deltaTime / this._deployTime;
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
		this.Unlink();
	}

	private void SetupLink()
	{
		if (base.Hub.roleManager.CurrentRole is IFpcRole fpcRole && fpcRole.FpcModule.CharacterModelInstance is AnimatedCharacterModel originalCharacterModel)
		{
			this._instanceSet = true;
			this._originalCharacterModel = originalCharacterModel;
			this._parent = base.Hub.transform;
			this._marshmallowModelInstance = Object.Instantiate(this._marshmallowModelTemplate, this._parent);
			this._marshmallowAudio = this._marshmallowModelInstance.GetComponent<MarshmallowAudio>();
			this._marshmallowModelInstance.Pooled = false;
			this._marshmallowModelInstance.Setup(base.Hub, fpcRole, this._marshmallowModelTemplate.transform.position, this._marshmallowModelTemplate.transform.rotation);
			this._originalCharacterModel.OnVisibilityChanged += OnVisibilityChanged;
			this._originalCharacterModel.OnFadeChanged += OnFadeChanged;
			this.UpdateMaterials();
			this.OnVisibilityChanged();
		}
	}

	private void Unlink()
	{
		if (this._instanceSet)
		{
			this._originalCharacterModel.OnVisibilityChanged -= OnVisibilityChanged;
			this._originalCharacterModel.OnFadeChanged -= OnFadeChanged;
			this.SetShaderFloat(this._originalCharacterModel, MarshmallowEffect.FadeHash, this._originalCharacterModel.Fade);
			this._marshmallowModelInstance.Pooled = true;
			this._marshmallowModelInstance.ResetObject();
			Object.Destroy(this._marshmallowModelInstance.gameObject);
			this._instanceSet = false;
		}
	}

	private void UpdateMaterials()
	{
		if (this._instanceSet)
		{
			float deployProgress = this.DeployProgress;
			this.SetShaderFloat(this._marshmallowModelInstance, MarshmallowEffect.DeployAnimHash, deployProgress);
			this.SetShaderFloat(this._originalCharacterModel, MarshmallowEffect.FadeHash, this._originalFadeOverProgress.Evaluate(deployProgress));
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
			this._turningBack = true;
		}
	}

	private void OnSwing(ushort serial)
	{
		if (serial == base.Hub.inventory.CurItem.SerialNumber)
		{
			this._marshmallowModelInstance.Animator.SetBool(MarshmallowEffect.AttackMirrorHash, this._mirrorAttack);
			this._marshmallowModelInstance.Animator.SetTrigger(MarshmallowEffect.AttackTriggerHash);
			this._mirrorAttack = !this._mirrorAttack;
		}
	}

	private void OnVisibilityChanged()
	{
		if (this._instanceSet)
		{
			this._marshmallowModelInstance.SetVisibility(this._originalCharacterModel.IsVisible);
		}
	}

	private void OnFadeChanged()
	{
		if (this._instanceSet)
		{
			this._marshmallowModelInstance.Fade = this._originalCharacterModel.Fade;
			this.UpdateMaterials();
		}
	}
}

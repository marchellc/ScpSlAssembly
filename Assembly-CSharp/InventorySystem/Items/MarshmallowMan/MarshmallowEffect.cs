using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerStatsSystem;
using UnityEngine;

namespace InventorySystem.Items.MarshmallowMan
{
	public class MarshmallowEffect : StatusEffectBase, IDamageModifierEffect, IMovementSpeedModifier, IStaminaModifier, IFriendlyFireModifier
	{
		private float DeployProgress
		{
			get
			{
				return this._progress;
			}
			set
			{
				float num = Mathf.Clamp01(value);
				if (num == this._progress)
				{
					return;
				}
				this._progress = num;
				this.UpdateMaterials();
			}
		}

		public bool DamageModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return 1.55f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				if (base.Intensity != 255 || !base.IsLocalPlayer)
				{
					return float.MaxValue;
				}
				return 0.5f;
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return true;
			}
		}

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
			MarshmallowItem.OnSwing += this.OnSwing;
			MarshmallowItem.OnHolsterRequested += this.OnHolsterRequested;
			if (!NetworkServer.active)
			{
				return;
			}
			base.Hub.inventory.ServerDropEverything();
			foreach (KeyValuePair<ItemType, ItemBase> keyValuePair in InventoryItemLoader.AvailableItems)
			{
				if (keyValuePair.Value is MarshmallowItem)
				{
					base.Hub.inventory.ServerAddItem(keyValuePair.Key, ItemAddReason.StatusEffect, 0, null);
					break;
				}
			}
		}

		protected override void Disabled()
		{
			base.Disabled();
			MarshmallowItem.OnSwing -= this.OnSwing;
			MarshmallowItem.OnHolsterRequested -= this.OnHolsterRequested;
			this.Unlink();
			this._progress = 0f;
			this._turningBack = false;
			this._mirrorAttack = false;
			if (!NetworkServer.active)
			{
				return;
			}
			Inventory inventory = base.Hub.inventory;
			if (!(inventory.CurInstance is MarshmallowItem))
			{
				return;
			}
			inventory.ServerDropItem(inventory.CurItem.SerialNumber);
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
			if (!NetworkServer.active || base.Duration == 0f || base.TimeLeft > 1.1f || !base.IsEnabled)
			{
				return;
			}
			base.Intensity = byte.MaxValue;
		}

		protected override void IntensityChanged(byte prevState, byte newState)
		{
			base.IntensityChanged(prevState, newState);
			if (!NetworkServer.active)
			{
				return;
			}
			if (newState != 255 || prevState == 0)
			{
				return;
			}
			MarshmallowItem marshmallowItem = base.Hub.inventory.CurInstance as MarshmallowItem;
			if (marshmallowItem == null || marshmallowItem == null)
			{
				return;
			}
			marshmallowItem.ServerRequestHolster();
		}

		private void OnDestroy()
		{
			this.Unlink();
		}

		private void SetupLink()
		{
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return;
			}
			AnimatedCharacterModel animatedCharacterModel = fpcRole.FpcModule.CharacterModelInstance as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return;
			}
			this._instanceSet = true;
			this._originalCharacterModel = animatedCharacterModel;
			this._parent = base.Hub.transform;
			this._marshmallowModelInstance = global::UnityEngine.Object.Instantiate<AnimatedCharacterModel>(this._marshmallowModelTemplate, this._parent);
			this._marshmallowAudio = this._marshmallowModelInstance.GetComponent<MarshmallowAudio>();
			this._marshmallowModelInstance.Pooled = false;
			this._marshmallowModelInstance.Setup(base.Hub, fpcRole, this._marshmallowModelTemplate.transform.position, this._marshmallowModelTemplate.transform.rotation);
			this._originalCharacterModel.OnVisibilityChanged += this.OnVisibilityChanged;
			this._originalCharacterModel.OnFadeChanged += this.OnFadeChanged;
			this.UpdateMaterials();
			this.OnVisibilityChanged();
		}

		private void Unlink()
		{
			if (!this._instanceSet)
			{
				return;
			}
			this._originalCharacterModel.OnVisibilityChanged -= this.OnVisibilityChanged;
			this._originalCharacterModel.OnFadeChanged -= this.OnFadeChanged;
			this.SetShaderFloat(this._originalCharacterModel, MarshmallowEffect.FadeHash, this._originalCharacterModel.Fade);
			this._marshmallowModelInstance.Pooled = true;
			this._marshmallowModelInstance.ResetObject();
			global::UnityEngine.Object.Destroy(this._marshmallowModelInstance.gameObject);
			this._instanceSet = false;
		}

		private void UpdateMaterials()
		{
			if (!this._instanceSet)
			{
				return;
			}
			float deployProgress = this.DeployProgress;
			this.SetShaderFloat(this._marshmallowModelInstance, MarshmallowEffect.DeployAnimHash, deployProgress);
			this.SetShaderFloat(this._originalCharacterModel, MarshmallowEffect.FadeHash, this._originalFadeOverProgress.Evaluate(deployProgress));
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
			if (serial != base.Hub.inventory.CurItem.SerialNumber)
			{
				return;
			}
			this._turningBack = true;
		}

		private void OnSwing(ushort serial)
		{
			if (serial != base.Hub.inventory.CurItem.SerialNumber)
			{
				return;
			}
			this._marshmallowModelInstance.Animator.SetBool(MarshmallowEffect.AttackMirrorHash, this._mirrorAttack);
			this._marshmallowModelInstance.Animator.SetTrigger(MarshmallowEffect.AttackTriggerHash);
			this._mirrorAttack = !this._mirrorAttack;
		}

		private void OnVisibilityChanged()
		{
			if (!this._instanceSet)
			{
				return;
			}
			this._marshmallowModelInstance.SetVisibility(this._originalCharacterModel.IsVisible);
		}

		private void OnFadeChanged()
		{
			if (!this._instanceSet)
			{
				return;
			}
			this._marshmallowModelInstance.Fade = this._originalCharacterModel.Fade;
			this.UpdateMaterials();
		}

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

		private const byte TurnBackIntensityCode = 255;
	}
}

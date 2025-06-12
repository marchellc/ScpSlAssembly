using System.Collections.Generic;
using System.Diagnostics;
using InventorySystem.GUI;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Attachments.Components;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using UserSettings;
using UserSettings.ControlsSettings;

namespace InventorySystem.Items.Firearms.Modules;

public class LinearAdsModule : ModuleBase, IAdsModule, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule, IZoomModifyingItem, IInspectPreventerModule, IMovementSpeedModifier, IStaminaModifier, ISwayModifierModule
{
	private class AdsData
	{
		private float _adjustSpeed;

		private float _lastOffset;

		private readonly Stopwatch _lastUpdate = Stopwatch.StartNew();

		public bool AdsTarget { get; private set; }

		public float AdsAmount
		{
			get
			{
				double num = this._lastUpdate.Elapsed.TotalSeconds * (double)this._adjustSpeed;
				double num2 = 1f - this._lastOffset;
				float num3 = Mathf.Clamp01((float)(num + num2));
				if (!this.AdsTarget)
				{
					return 1f - num3;
				}
				return num3;
			}
		}

		public void Update(bool target, float speed, out bool targetChanged, out bool speedChanged)
		{
			speedChanged = speed != this._adjustSpeed;
			targetChanged = this.AdsTarget != target;
			if (speedChanged)
			{
				this._adjustSpeed = speed;
			}
			if (targetChanged)
			{
				this._lastOffset = this.AdsAmount;
				this._lastUpdate.Restart();
				this.AdsTarget = target;
				if (this.AdsTarget)
				{
					this._lastOffset = 1f - this._lastOffset;
				}
			}
		}

		public void DecodeData(NetworkReader reader)
		{
			float num = reader.ReadFloat();
			bool target = num > 0f;
			float speed = Mathf.Abs(num);
			this.Update(target, speed, out var _, out var _);
		}

		public void Reset()
		{
			this.AdsTarget = false;
			this._lastOffset = 0f;
		}
	}

	private const float FallbackAdsZoom = 1.15f;

	private const float FallbackAdsInaccuracy = 0.17f;

	private const float FallbackHipInaccuracy = 2f;

	private const float FallbackTimeToTransition = 0.2f;

	private const float TransitioningPenaltyDegrees = 3f;

	private const float AdsJumpSwayMultiplier = 0.3f;

	private static readonly Remap MovementLimitRemap = new Remap(7f, 45f, 6f, 1.7f);

	private static readonly Dictionary<FirearmCategory, float> HipInaccuracyByCategory = new Dictionary<FirearmCategory, float>
	{
		[FirearmCategory.Pistol] = 1.7f,
		[FirearmCategory.Revolver] = 1.7f,
		[FirearmCategory.SubmachineGun] = 1.7f,
		[FirearmCategory.Rifle] = 1.77f,
		[FirearmCategory.LightMachineGun] = 2.2f,
		[FirearmCategory.Shotgun] = 1.2f
	};

	private static readonly Dictionary<FirearmCategory, float> AdsInaccuracyByCategory = new Dictionary<FirearmCategory, float>
	{
		[FirearmCategory.Pistol] = 0.25f,
		[FirearmCategory.Revolver] = 0.21f,
		[FirearmCategory.SubmachineGun] = 0.17f,
		[FirearmCategory.Rifle] = 0.07f,
		[FirearmCategory.LightMachineGun] = 0.13f,
		[FirearmCategory.Shotgun] = 0.2f
	};

	private static readonly Dictionary<FirearmCategory, float> TimeToTransitionByCategory = new Dictionary<FirearmCategory, float>
	{
		[FirearmCategory.Rifle] = 0.25f,
		[FirearmCategory.SubmachineGun] = 0.28f,
		[FirearmCategory.Shotgun] = 0.18f
	};

	private static readonly ToggleOrHoldInput AdsInput = new ToggleOrHoldInput(ActionName.Zoom, new CachedUserSetting<bool>(MiscControlsSetting.AdsToggle));

	private static readonly Dictionary<ushort, AdsData> SyncData = new Dictionary<ushort, AdsData>();

	private bool _userInput;

	private bool _hasZoomOptions;

	private readonly AdsData _clientData = new AdsData();

	private float EffectiveHipInaccuracy => this.BaseHipInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.HipInaccuracyMultiplier);

	private float EffectiveAdsInaccuracy => this.BaseAdsInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.AdsInaccuracyMultiplier);

	protected virtual bool AllowAds
	{
		get
		{
			ModuleBase[] modules = base.Firearm.Modules;
			foreach (ModuleBase moduleBase in modules)
			{
				if (moduleBase is IEquipperModule { IsEquipped: false })
				{
					return false;
				}
				if (moduleBase is IReloaderModule { IsReloadingOrUnloading: not false })
				{
					return false;
				}
				if (moduleBase is IAdsPreventerModule { AdsAllowed: false })
				{
					return false;
				}
			}
			return true;
		}
	}

	protected virtual bool ForceAdsInput => false;

	public DisplayInaccuracyValues DisplayInaccuracy => new DisplayInaccuracyValues(this.EffectiveHipInaccuracy, this.EffectiveAdsInaccuracy, this.EffectiveHipInaccuracy);

	public bool AdsTarget
	{
		get
		{
			if (!base.IsLocalPlayer)
			{
				return LinearAdsModule.GetAdsTargetForSerial(base.ItemSerial);
			}
			return this._clientData.AdsTarget;
		}
	}

	public float AdsAmount
	{
		get
		{
			if (!base.IsLocalPlayer)
			{
				return LinearAdsModule.GetAdsAmountForSerial(base.Firearm.ItemSerial);
			}
			return this._clientData.AdsAmount;
		}
	}

	public float Inaccuracy
	{
		get
		{
			float num = Mathf.Lerp(this.EffectiveHipInaccuracy, this.EffectiveAdsInaccuracy, this.AdsAmount);
			float num2 = Mathf.Abs(this.AdsAmount - 0.5f) * 2f;
			float num3 = num2 * num2 * num2 * num2;
			float num4 = 1f - num3;
			return num + num4 * 3f;
		}
	}

	public bool InspectionAllowed => this.AdsAmount == 0f;

	public virtual float BaseHipInaccuracy
	{
		get
		{
			if (!LinearAdsModule.HipInaccuracyByCategory.TryGetValue(base.Firearm.FirearmCategory, out var value))
			{
				return 2f;
			}
			return value;
		}
	}

	public virtual float BaseAdsInaccuracy
	{
		get
		{
			if (!LinearAdsModule.AdsInaccuracyByCategory.TryGetValue(base.Firearm.FirearmCategory, out var value))
			{
				return 0.17f;
			}
			return value;
		}
	}

	public virtual float BaseTimeToTransition
	{
		get
		{
			if (!LinearAdsModule.TimeToTransitionByCategory.TryGetValue(base.Firearm.FirearmCategory, out var value))
			{
				return 0.2f;
			}
			return value;
		}
	}

	public virtual float BaseZoomAmount
	{
		get
		{
			if (!this._hasZoomOptions)
			{
				return 1.15f;
			}
			return 1f;
		}
	}

	public virtual float ZoomAmount
	{
		get
		{
			float num = base.Firearm.AttachmentsValue(AttachmentParam.AdsZoomMultiplier);
			return (this.BaseZoomAmount * num - 1f) * Mathf.SmoothStep(0f, 1f, this.AdsAmount) + 1f;
		}
	}

	public virtual float AdditionalSensitivityModifier => base.Firearm.AttachmentsValue(AttachmentParam.AdsMouseSensitivityMultiplier);

	public virtual float SensitivityScale
	{
		get
		{
			float num = base.Firearm.AttachmentsValue(AttachmentParam.AdsZoomMultiplier);
			float num2 = this.BaseZoomAmount * num * this.AdditionalSensitivityModifier;
			float adsReductionMultiplier = SensitivitySettings.AdsReductionMultiplier;
			float num3 = this.AdsAmount;
			if (adsReductionMultiplier < 1f)
			{
				num3 *= adsReductionMultiplier;
			}
			else
			{
				num2 *= adsReductionMultiplier;
			}
			return 1f / Mathf.Lerp(1f, num2, num3);
		}
	}

	public bool IsTransitioning
	{
		get
		{
			if (this.AdsAmount > 0f)
			{
				return this.AdsAmount < 1f;
			}
			return false;
		}
	}

	public bool MovementModifierActive
	{
		get
		{
			if (base.IsLocalPlayer)
			{
				return this.AdsAmount > 0f;
			}
			return false;
		}
	}

	public float MovementSpeedMultiplier => 1f;

	public float MovementSpeedLimit => LinearAdsModule.MovementLimitRemap.Get(this.AdsAmount * base.Firearm.Length);

	public bool StaminaModifierActive => this.MovementModifierActive;

	public bool SprintingDisabled
	{
		get
		{
			if (base.Firearm.Owner.roleManager.CurrentRole is IFpcRole fpcRole)
			{
				return this.MovementSpeedLimit <= fpcRole.FpcModule.VelocityForState(PlayerMovementState.Walking, applyCrouch: false);
			}
			return false;
		}
	}

	public float WalkSwayScale => 1f - this.AdsAmount;

	public float JumpSwayScale => Mathf.Lerp(1f, 0.3f, this.AdsAmount);

	protected virtual void OnAdsChanged(bool newTarget, float newSpeed, bool targetChanged, bool speedChanged)
	{
		if (!targetChanged && !speedChanged)
		{
			return;
		}
		if (base.IsLocalPlayer)
		{
			this.SendCmd(delegate(NetworkWriter x)
			{
				x.WriteBool(this._userInput);
			});
		}
		if (base.IsServer)
		{
			this.SendRpc(delegate(NetworkWriter x)
			{
				x.WriteFloat(newTarget ? newSpeed : (0f - newSpeed));
			});
		}
	}

	internal override void OnClientReady()
	{
		base.OnClientReady();
		LinearAdsModule.SyncData.Clear();
	}

	internal override void EquipUpdate()
	{
		base.EquipUpdate();
		if (!base.Firearm.HasOwner)
		{
			return;
		}
		AdsData adsData;
		if (base.IsControllable)
		{
			adsData = this._clientData;
			if (base.Item.IsDummy)
			{
				this._userInput = base.GetAction(ActionName.Zoom);
			}
			else if (InventoryGuiController.ItemsSafeForInteraction)
			{
				this._userInput = LinearAdsModule.AdsInput.IsActive;
			}
		}
		else
		{
			if (!base.IsServer)
			{
				return;
			}
			adsData = LinearAdsModule.SyncData.GetOrAdd(base.ItemSerial, () => new AdsData());
		}
		bool flag = (this._userInput || this.ForceAdsInput) && this.AllowAds;
		float num = base.Firearm.AttachmentsValue(AttachmentParam.AdsSpeedMultiplier) / this.BaseTimeToTransition;
		adsData.Update(flag, num, out var targetChanged, out var speedChanged);
		this.OnAdsChanged(flag, num, targetChanged, speedChanged);
	}

	protected override void OnInit()
	{
		base.OnInit();
		Attachment[] attachments = base.Firearm.Attachments;
		for (int i = 0; i < attachments.Length; i++)
		{
			if (attachments[i].TryGetActiveValue(AttachmentParam.AdsZoomMultiplier, out var _))
			{
				this._hasZoomOptions = true;
				break;
			}
		}
	}

	internal override void OnHolstered()
	{
		base.OnHolstered();
		if (base.IsLocalPlayer)
		{
			this._clientData.Reset();
			LinearAdsModule.AdsInput.ResetToggle();
		}
		if (base.IsServer && this.AdsTarget)
		{
			this.SendRpc();
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		this._userInput = reader.ReadBool();
		PlayerEvents.OnAimedWeapon(new PlayerAimedWeaponEventArgs(base.Firearm.Owner, base.Firearm, this._userInput));
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		if (reader.Remaining == 0)
		{
			if (LinearAdsModule.SyncData.TryGetValue(serial, out var value))
			{
				value.Reset();
			}
		}
		else
		{
			LinearAdsModule.SyncData.GetOrAdd(serial, () => new AdsData()).DecodeData(reader);
		}
	}

	public void GetDisplayAdsValues(ushort serial, out bool adsTarget, out float adsAmount)
	{
		if (LinearAdsModule.SyncData.TryGetValue(serial, out var value))
		{
			adsTarget = value.AdsTarget;
			adsAmount = value.AdsAmount;
		}
		else
		{
			adsTarget = false;
			adsAmount = 0f;
		}
	}

	public static bool GetAdsTargetForSerial(ushort serial)
	{
		if (LinearAdsModule.SyncData.TryGetValue(serial, out var value))
		{
			return value.AdsTarget;
		}
		return false;
	}

	public static float GetAdsAmountForSerial(ushort serial)
	{
		if (!LinearAdsModule.SyncData.TryGetValue(serial, out var value))
		{
			return 0f;
		}
		return value.AdsAmount;
	}
}

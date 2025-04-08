using System;
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

namespace InventorySystem.Items.Firearms.Modules
{
	public class LinearAdsModule : ModuleBase, IAdsModule, IDisplayableInaccuracyProviderModule, IInaccuracyProviderModule, IZoomModifyingItem, IInspectPreventerModule, IMovementSpeedModifier, IStaminaModifier, ISwayModifierModule
	{
		private float EffectiveHipInaccuracy
		{
			get
			{
				return this.BaseHipInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.HipInaccuracyMultiplier);
			}
		}

		private float EffectiveAdsInaccuracy
		{
			get
			{
				return this.BaseAdsInaccuracy * base.Firearm.AttachmentsValue(AttachmentParam.AdsInaccuracyMultiplier);
			}
		}

		protected virtual bool AllowAds
		{
			get
			{
				foreach (ModuleBase moduleBase in base.Firearm.Modules)
				{
					IEquipperModule equipperModule = moduleBase as IEquipperModule;
					if (equipperModule != null && !equipperModule.IsEquipped)
					{
						return false;
					}
					IReloaderModule reloaderModule = moduleBase as IReloaderModule;
					if (reloaderModule != null && reloaderModule.IsReloadingOrUnloading)
					{
						return false;
					}
					IAdsPreventerModule adsPreventerModule = moduleBase as IAdsPreventerModule;
					if (adsPreventerModule != null && !adsPreventerModule.AdsAllowed)
					{
						return false;
					}
				}
				return true;
			}
		}

		protected virtual bool ForceAdsInput
		{
			get
			{
				return false;
			}
		}

		public DisplayInaccuracyValues DisplayInaccuracy
		{
			get
			{
				return new DisplayInaccuracyValues(this.EffectiveHipInaccuracy, this.EffectiveAdsInaccuracy, this.EffectiveHipInaccuracy, 0f);
			}
		}

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

		public bool InspectionAllowed
		{
			get
			{
				return this.AdsAmount == 0f;
			}
		}

		public virtual float BaseHipInaccuracy
		{
			get
			{
				float num;
				if (!LinearAdsModule.HipInaccuracyByCategory.TryGetValue(base.Firearm.FirearmCategory, out num))
				{
					return 2f;
				}
				return num;
			}
		}

		public virtual float BaseAdsInaccuracy
		{
			get
			{
				float num;
				if (!LinearAdsModule.AdsInaccuracyByCategory.TryGetValue(base.Firearm.FirearmCategory, out num))
				{
					return 0.17f;
				}
				return num;
			}
		}

		public virtual float BaseTimeToTransition
		{
			get
			{
				float num;
				if (!LinearAdsModule.TimeToTransitionByCategory.TryGetValue(base.Firearm.FirearmCategory, out num))
				{
					return 0.2f;
				}
				return num;
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

		public virtual float AdditionalSensitivityModifier
		{
			get
			{
				return base.Firearm.AttachmentsValue(AttachmentParam.AdsMouseSensitivityMultiplier);
			}
		}

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
				return this.AdsAmount > 0f && this.AdsAmount < 1f;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return base.IsLocalPlayer && this.AdsAmount > 0f;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return 1f;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return LinearAdsModule.MovementLimitRemap.Get(this.AdsAmount * base.Firearm.Length);
			}
		}

		public bool StaminaModifierActive
		{
			get
			{
				return this.MovementModifierActive;
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				IFpcRole fpcRole = base.Firearm.Owner.roleManager.CurrentRole as IFpcRole;
				return fpcRole != null && this.MovementSpeedLimit <= fpcRole.FpcModule.VelocityForState(PlayerMovementState.Walking, false);
			}
		}

		public float WalkSwayScale
		{
			get
			{
				return 1f - this.AdsAmount;
			}
		}

		public float JumpSwayScale
		{
			get
			{
				return Mathf.Lerp(1f, 0.3f, this.AdsAmount);
			}
		}

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
					x.WriteFloat(newTarget ? newSpeed : (-newSpeed));
				}, true);
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
			LinearAdsModule.AdsData adsData;
			if (base.IsLocalPlayer)
			{
				adsData = this._clientData;
				if (InventoryGuiController.ItemsSafeForInteraction)
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
				adsData = LinearAdsModule.SyncData.GetOrAdd(base.ItemSerial, () => new LinearAdsModule.AdsData());
			}
			bool flag = (this._userInput || this.ForceAdsInput) && this.AllowAds;
			float num = base.Firearm.AttachmentsValue(AttachmentParam.AdsSpeedMultiplier) / this.BaseTimeToTransition;
			bool flag2;
			bool flag3;
			adsData.Update(flag, num, out flag2, out flag3);
			this.OnAdsChanged(flag, num, flag2, flag3);
		}

		protected override void OnInit()
		{
			base.OnInit();
			Attachment[] attachments = base.Firearm.Attachments;
			for (int i = 0; i < attachments.Length; i++)
			{
				float num;
				if (attachments[i].TryGetActiveValue(AttachmentParam.AdsZoomMultiplier, out num))
				{
					this._hasZoomOptions = true;
					return;
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
				this.SendRpc(null, true);
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
			if (reader.Remaining != 0)
			{
				LinearAdsModule.SyncData.GetOrAdd(serial, () => new LinearAdsModule.AdsData()).DecodeData(reader);
				return;
			}
			LinearAdsModule.AdsData adsData;
			if (!LinearAdsModule.SyncData.TryGetValue(serial, out adsData))
			{
				return;
			}
			adsData.Reset();
		}

		public void GetDisplayAdsValues(ushort serial, out bool adsTarget, out float adsAmount)
		{
			LinearAdsModule.AdsData adsData;
			if (LinearAdsModule.SyncData.TryGetValue(serial, out adsData))
			{
				adsTarget = adsData.AdsTarget;
				adsAmount = adsData.AdsAmount;
				return;
			}
			adsTarget = false;
			adsAmount = 0f;
		}

		public static bool GetAdsTargetForSerial(ushort serial)
		{
			LinearAdsModule.AdsData adsData;
			return LinearAdsModule.SyncData.TryGetValue(serial, out adsData) && adsData.AdsTarget;
		}

		public static float GetAdsAmountForSerial(ushort serial)
		{
			LinearAdsModule.AdsData adsData;
			if (!LinearAdsModule.SyncData.TryGetValue(serial, out adsData))
			{
				return 0f;
			}
			return adsData.AdsAmount;
		}

		// Note: this type is marked as 'beforefieldinit'.
		static LinearAdsModule()
		{
			Dictionary<FirearmCategory, float> dictionary = new Dictionary<FirearmCategory, float>();
			dictionary[FirearmCategory.Pistol] = 1.7f;
			dictionary[FirearmCategory.Revolver] = 1.7f;
			dictionary[FirearmCategory.SubmachineGun] = 1.7f;
			dictionary[FirearmCategory.Rifle] = 1.77f;
			dictionary[FirearmCategory.LightMachineGun] = 2.2f;
			dictionary[FirearmCategory.Shotgun] = 1.2f;
			LinearAdsModule.HipInaccuracyByCategory = dictionary;
			Dictionary<FirearmCategory, float> dictionary2 = new Dictionary<FirearmCategory, float>();
			dictionary2[FirearmCategory.Pistol] = 0.25f;
			dictionary2[FirearmCategory.Revolver] = 0.21f;
			dictionary2[FirearmCategory.SubmachineGun] = 0.17f;
			dictionary2[FirearmCategory.Rifle] = 0.07f;
			dictionary2[FirearmCategory.LightMachineGun] = 0.13f;
			dictionary2[FirearmCategory.Shotgun] = 0.2f;
			LinearAdsModule.AdsInaccuracyByCategory = dictionary2;
			Dictionary<FirearmCategory, float> dictionary3 = new Dictionary<FirearmCategory, float>();
			dictionary3[FirearmCategory.Rifle] = 0.25f;
			dictionary3[FirearmCategory.SubmachineGun] = 0.28f;
			dictionary3[FirearmCategory.Shotgun] = 0.18f;
			LinearAdsModule.TimeToTransitionByCategory = dictionary3;
			LinearAdsModule.AdsInput = new ToggleOrHoldInput(ActionName.Zoom, new CachedUserSetting<bool>(MiscControlsSetting.AdsToggle), ToggleOrHoldInput.InputActivationMode.Toggle);
			LinearAdsModule.SyncData = new Dictionary<ushort, LinearAdsModule.AdsData>();
		}

		private const float FallbackAdsZoom = 1.15f;

		private const float FallbackAdsInaccuracy = 0.17f;

		private const float FallbackHipInaccuracy = 2f;

		private const float FallbackTimeToTransition = 0.2f;

		private const float TransitioningPenaltyDegrees = 3f;

		private const float AdsJumpSwayMultiplier = 0.3f;

		private static readonly Remap MovementLimitRemap = new Remap(7f, 45f, 6f, 1.7f, true);

		private static readonly Dictionary<FirearmCategory, float> HipInaccuracyByCategory;

		private static readonly Dictionary<FirearmCategory, float> AdsInaccuracyByCategory;

		private static readonly Dictionary<FirearmCategory, float> TimeToTransitionByCategory;

		private static readonly ToggleOrHoldInput AdsInput;

		private static readonly Dictionary<ushort, LinearAdsModule.AdsData> SyncData;

		private bool _userInput;

		private bool _hasZoomOptions;

		private readonly LinearAdsModule.AdsData _clientData = new LinearAdsModule.AdsData();

		private class AdsData
		{
			public bool AdsTarget { get; private set; }

			public float AdsAmount
			{
				get
				{
					float num = (float)(this._lastUpdate.Elapsed.TotalSeconds * (double)this._adjustSpeed);
					double num2 = (double)(1f - this._lastOffset);
					float num3 = Mathf.Clamp01((float)((double)num + num2));
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
				bool flag = num > 0f;
				float num2 = Mathf.Abs(num);
				bool flag2;
				bool flag3;
				this.Update(flag, num2, out flag2, out flag3);
			}

			public void Reset()
			{
				this.AdsTarget = false;
				this._lastOffset = 0f;
			}

			private float _adjustSpeed;

			private float _lastOffset;

			private readonly Stopwatch _lastUpdate = Stopwatch.StartNew();
		}
	}
}

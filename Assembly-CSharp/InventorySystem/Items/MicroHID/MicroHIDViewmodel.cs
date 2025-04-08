using System;
using AudioPooling;
using InventorySystem.Items.MicroHID.Modules;
using InventorySystem.Items.SwayControllers;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDViewmodel : StandardAnimatedViemodel
	{
		private bool IsInspecting
		{
			get
			{
				AnimatorStateInfo animatorStateInfo = this.AnimatorStateInfo(0);
				return animatorStateInfo.tagHash == MicroHIDViewmodel.IdleTagNameHash || animatorStateInfo.tagHash == MicroHIDViewmodel.InspectTagNameHash;
			}
		}

		private bool ValidateStartInspect
		{
			get
			{
				AnimatorStateInfo animatorStateInfo = this.AnimatorStateInfo(0);
				bool flag = this.AnimatorInTransition(0);
				return animatorStateInfo.tagHash == MicroHIDViewmodel.IdleTagNameHash && !flag;
			}
		}

		private float WindupShakeAmount
		{
			get
			{
				float progress = WindupSyncModule.GetProgress(base.ItemId.SerialNumber);
				return this._shakingOverWindupProgress.Evaluate(progress);
			}
		}

		private float SustainShakeAmount
		{
			get
			{
				if (this._cycle.Phase != MicroHidPhase.WoundUpSustain)
				{
					return 0f;
				}
				return this._shakingOverSustain.Evaluate(this._cycle.CurrentPhaseElapsed);
			}
		}

		protected override IItemSwayController GetNewSwayController()
		{
			return new WalkSway(new GoopSway.GoopSwaySettings(this.HandsPivot, 1.6f, 0.0015f, 0.04f, 4f, 6.5f, 0.025f, 2.6f, false), this);
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			base.InitSpectator(ply, id, wasEquipped);
			this.UpdateAllAnims();
			if (!wasEquipped)
			{
				return;
			}
			this.AnimatorForceUpdate(base.SkipEquipTime, false);
		}

		public override void InitAny()
		{
			base.InitAny();
			BrokenSyncModule.OnBroken += this.OnBroken;
			DrawAndInspectorModule.OnInspectRequested += this.OnInspectRequested;
			if (BrokenSyncModule.GetBroken(base.ItemId.SerialNumber))
			{
				this._brokenElapsed = this._brokenWeightOverTime[this._brokenWeightOverTime.length - 1].time;
			}
			this._cycle = CycleSyncModule.GetCycleController(base.ItemId.SerialNumber);
			this._particles.Init(base.ItemId.SerialNumber, base.Hub.PlayerCameraReference);
			foreach (ParticleSystem particleSystem in this._particles.GetComponentsInChildren<ParticleSystem>(true))
			{
				if (particleSystem.gameObject.layer == base.gameObject.layer)
				{
					WorldspaceLightParticleConverter.Convert(particleSystem, false);
				}
			}
		}

		private void OnBroken(ushort serial)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.AnimatorSetLayerWeight(3, 1f);
			this.AnimatorSetTrigger(MicroHIDViewmodel.BrokenHash);
		}

		private void OnInspectRequested(ushort serial)
		{
			if (serial != base.ItemId.SerialNumber)
			{
				return;
			}
			if (!this.ValidateStartInspect)
			{
				return;
			}
			this.AnimatorSetTrigger(MicroHIDViewmodel.InspectStartTriggerHash);
			this.AnimatorSetBool(MicroHIDViewmodel.InspectValidBoolHash, true);
			this._inspectSoundSession = new AudioPoolSession(AudioSourcePoolManager.Play2DWithParent(this._inspectSound, base.transform, 1f, MixerChannel.DefaultSfx, 1f));
		}

		private void OnDestroy()
		{
			BrokenSyncModule.OnBroken -= this.OnBroken;
			DrawAndInspectorModule.OnInspectRequested -= this.OnInspectRequested;
		}

		private void Update()
		{
			this.UpdateAllAnims();
		}

		private void UpdateAllAnims()
		{
			this.UpdateBroken();
			this.UpdatePickup();
			this.UpdateInspect();
			this.UpdateCycle();
			this.UpdateShaking();
		}

		private void UpdateBroken()
		{
			if (BrokenSyncModule.GetBroken(base.ItemId.SerialNumber))
			{
				this._brokenElapsed += Time.deltaTime;
			}
			this.AnimatorSetLayerWeight(3, this._brokenWeightOverTime.Evaluate(this._brokenElapsed));
		}

		private void UpdatePickup()
		{
			bool flag = DrawAndInspectorModule.CheckPickupPreference(base.ItemId.SerialNumber);
			this.AnimatorSetFloat(MicroHIDViewmodel.FirstTimePickupFloatHash, (float)(flag ? 1 : 0));
		}

		private void UpdateInspect()
		{
			if (!this.IsInspecting && this._inspectSoundSession.SameSession)
			{
				this._inspectSoundSession.Source.volume -= Time.deltaTime * 3f;
			}
			this.AnimatorSetBool(MicroHIDViewmodel.InspectValidBoolHash, this.IsInspecting);
		}

		private void UpdateCycle()
		{
			this.AnimatorSetInt(MicroHIDViewmodel.CurPhaseHash, (int)this._cycle.Phase);
			this.AnimatorSetInt(MicroHIDViewmodel.FiringModeHash, (int)this._cycle.LastFiringMode);
			MicroHidPhase? prevPhase = this._prevPhase;
			MicroHidPhase phase = this._cycle.Phase;
			if (!((prevPhase.GetValueOrDefault() == phase) & (prevPhase != null)))
			{
				this._prevPhase = new MicroHidPhase?(this._cycle.Phase);
				this.AnimatorSetTrigger(MicroHIDViewmodel.PhaseChangedHash);
			}
		}

		private void UpdateShaking()
		{
			float num = this.AnimatorGetLayerWeight(4);
			float num2 = this.WindupShakeAmount + this.SustainShakeAmount;
			float num3 = Time.deltaTime * 2f;
			float num4 = Mathf.MoveTowards(num, num2, num3);
			this.AnimatorSetLayerWeight(4, num4);
		}

		private static readonly int FiringModeHash = Animator.StringToHash("LastFiringMode");

		private static readonly int CurPhaseHash = Animator.StringToHash("CurPhase");

		private static readonly int PhaseChangedHash = Animator.StringToHash("OnPhaseChanged");

		private static readonly int InspectValidBoolHash = Animator.StringToHash("InspectValid");

		private static readonly int InspectStartTriggerHash = Animator.StringToHash("InspectStart");

		private static readonly int InspectTagNameHash = Animator.StringToHash("Inspect");

		private static readonly int FirstTimePickupFloatHash = Animator.StringToHash("DrawPickupBlend");

		private static readonly int IdleTagNameHash = Animator.StringToHash("Idle");

		private static readonly int BrokenHash = Animator.StringToHash("OnBroken");

		private const int InspectLayer = 0;

		private const int BrokenLayer = 3;

		private const int ShakeLayer = 4;

		private const int DecreaseVolumeSpeed = 3;

		private const int ShakeAdjustSpeed = 2;

		private MicroHidPhase? _prevPhase;

		private AudioPoolSession _inspectSoundSession;

		private float _brokenElapsed;

		private CycleController _cycle;

		[SerializeField]
		private AudioClip _inspectSound;

		[SerializeField]
		private MicroHIDParticles _particles;

		[SerializeField]
		private AnimationCurve _brokenWeightOverTime;

		[SerializeField]
		private AnimationCurve _shakingOverSustain;

		[SerializeField]
		private AnimationCurve _shakingOverWindupProgress;
	}
}

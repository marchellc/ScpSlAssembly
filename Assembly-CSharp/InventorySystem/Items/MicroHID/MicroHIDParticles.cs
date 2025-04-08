using System;
using InventorySystem.Items.MicroHID.Modules;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDParticles : MonoBehaviour
	{
		private bool WasFiring
		{
			get
			{
				float num;
				float num2;
				return this._cycle.TryGetElapsed(MicroHidPhase.WindingUp, out num) && this._cycle.TryGetElapsed(MicroHidPhase.Firing, out num2) && num > num2;
			}
		}

		public void Init(ushort serial, Transform camera)
		{
			this._initalized = true;
			this._serial = serial;
			this._camera = camera;
			this._cycle = CycleSyncModule.GetCycleController(serial);
		}

		private void Update()
		{
			if (!this._initalized)
			{
				return;
			}
			this.UpdateAll();
			this._prevPhase = this._cycle.Phase;
		}

		private void UpdateAll()
		{
			bool flag = this._cycle.Phase > MicroHidPhase.Standby;
			this._allParticles.SetActive(flag);
			if (!flag)
			{
				this._wasBroken = false;
				return;
			}
			this.UpdateBroken();
			this.UpdateFiring();
			float num = (this._wasBroken ? 0f : WindupSyncModule.GetProgress(this._serial));
			this._windupBlinkerSystem.main.startColor = this._windupBlinkerGradient.Evaluate(num);
			foreach (MicroHIDParticles.WindupArc windupArc in this._windupArcs)
			{
				windupArc.Update(num);
			}
		}

		private void UpdateFiring()
		{
			bool flag = this._cycle.Phase == MicroHidPhase.Firing;
			MicroHidPhase microHidPhase = this._prevPhase;
			bool flag2 = microHidPhase == MicroHidPhase.WindingUp || microHidPhase == MicroHidPhase.WoundUpSustain;
			bool flag3 = flag && flag2;
			float currentPhaseElapsed = this._cycle.CurrentPhaseElapsed;
			if (flag3)
			{
				ParticleSystem[] startFiringParticles = this._startFiringParticles;
				for (int i = 0; i < startFiringParticles.Length; i++)
				{
					startFiringParticles[i].Play(false);
				}
			}
			microHidPhase = this._cycle.Phase;
			float num;
			if (microHidPhase != MicroHidPhase.WindingDown)
			{
				if (microHidPhase == MicroHidPhase.Firing)
				{
					num = Mathf.Clamp01(currentPhaseElapsed / this._arcGrowTime);
					goto IL_00AD;
				}
			}
			else if (this.WasFiring)
			{
				num = Mathf.Clamp01(1f - currentPhaseElapsed / this._arcGrowTime);
				goto IL_00AD;
			}
			num = 0f;
			IL_00AD:
			FiringModeControllerModule firingModeControllerModule;
			if (num == 0f || !this._cycle.TryGetLastFiringController(out firingModeControllerModule))
			{
				this._chargeFireExtras.SetActive(false);
				foreach (MicroHIDParticles.AimedLaser aimedLaser in this._lasers)
				{
					aimedLaser.SetActive(false);
				}
				return;
			}
			this._chargeFireExtras.SetActive(firingModeControllerModule is ChargeFireModeModule);
			this._chargeFireExtras.transform.localScale = Vector3.up * num;
			foreach (MicroHIDParticles.AimedLaser aimedLaser2 in this._lasers)
			{
				aimedLaser2.SetActive(true);
				aimedLaser2.Update(this, firingModeControllerModule, num);
			}
		}

		private void UpdateBroken()
		{
			float num;
			if (this._wasBroken || !BrokenSyncModule.TryGetBrokenElapsed(this._serial, out num))
			{
				return;
			}
			if (num < 5f)
			{
				this._breakParticles.Simulate(num, true, true);
				if (this._breakParticles.isPaused)
				{
					this._breakParticles.Play();
				}
			}
			this._wasBroken = true;
		}

		private static readonly CachedLayerMask VisualizedCollisionMask = new CachedLayerMask(new string[] { "Default", "Glass", "Door" });

		[SerializeField]
		private GameObject _allParticles;

		[SerializeField]
		private float _rangeToLifetimeRatio;

		[SerializeField]
		private float _targetTrailWidth;

		[SerializeField]
		private ParticleSystem _breakParticles;

		[SerializeField]
		private MicroHIDParticles.AimedLaser[] _lasers;

		[SerializeField]
		private GameObject _chargeFireExtras;

		[SerializeField]
		private ParticleSystem[] _startFiringParticles;

		[SerializeField]
		private MicroHIDParticles.WindupArc[] _windupArcs;

		[SerializeField]
		private ParticleSystem _windupBlinkerSystem;

		[SerializeField]
		private Gradient _windupBlinkerGradient;

		[SerializeField]
		private float _arcGrowTime;

		private bool _initalized;

		private ushort _serial;

		private CycleController _cycle;

		private MicroHidPhase _prevPhase;

		private bool _wasBroken;

		private Transform _camera;

		private const float BreakAnimTime = 5f;

		[Serializable]
		private struct WindupArc
		{
			public readonly void Update(float windupProgress)
			{
				float num = this._emissionRateOverProgress.Evaluate(windupProgress);
				this._particleSys.emission.rateOverTime = new ParticleSystem.MinMaxCurve(num);
			}

			[SerializeField]
			private ParticleSystem _particleSys;

			[SerializeField]
			private AnimationCurve _emissionRateOverProgress;
		}

		[Serializable]
		private struct AimedLaser
		{
			public readonly void SetActive(bool b)
			{
				this._particleSys.gameObject.SetActive(b);
			}

			public readonly void Update(MicroHIDParticles parts, FiringModeControllerModule firingMode, float scale)
			{
				this._particleSys.trails.widthOverTrail = parts._targetTrailWidth * scale;
				float firingRange;
				if (this._viewmodelMode)
				{
					firingRange = firingMode.FiringRange;
				}
				else
				{
					Vector3 vector;
					this.GetTransformationData(parts._camera, firingMode.FiringRange, out firingRange, out vector);
					this._particleSys.transform.forward = vector;
				}
				this._particleSys.main.startLifetime = firingRange * parts._rangeToLifetimeRatio * scale;
			}

			private readonly void GetTransformationData(Transform camera, float firingRange, out float targetLength, out Vector3 targetFwd)
			{
				Vector3 position = camera.position;
				Vector3 forward = camera.forward;
				Transform transform = this._particleSys.transform;
				targetFwd = forward;
				targetLength = 0f;
				RaycastHit raycastHit;
				Vector3 vector = (Physics.Raycast(position, forward, out raycastHit, firingRange, MicroHIDParticles.VisualizedCollisionMask) ? raycastHit.point : (position + forward * firingRange)) - transform.position;
				float magnitude = vector.magnitude;
				if (magnitude == 0f)
				{
					return;
				}
				targetFwd = vector / magnitude;
				if (Vector3.Dot(targetFwd, forward) < 0.6f)
				{
					return;
				}
				targetLength = magnitude;
			}

			[SerializeField]
			private ParticleSystem _particleSys;

			[SerializeField]
			private bool _viewmodelMode;

			private const float MinDot = 0.6f;
		}
	}
}

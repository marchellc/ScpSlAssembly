using System;
using InventorySystem.Items.MicroHID.Modules;
using UnityEngine;

namespace InventorySystem.Items.MicroHID;

public class MicroHIDParticles : MonoBehaviour
{
	[Serializable]
	private struct WindupArc
	{
		[SerializeField]
		private ParticleSystem _particleSys;

		[SerializeField]
		private AnimationCurve _emissionRateOverProgress;

		public readonly void Update(float windupProgress)
		{
			float constant = this._emissionRateOverProgress.Evaluate(windupProgress);
			ParticleSystem.EmissionModule emission = this._particleSys.emission;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve(constant);
		}
	}

	[Serializable]
	private struct AimedLaser
	{
		[SerializeField]
		private ParticleSystem _particleSys;

		[SerializeField]
		private bool _viewmodelMode;

		private const float MinDot = 0.6f;

		public readonly void SetActive(bool b)
		{
			this._particleSys.gameObject.SetActive(b);
		}

		public readonly void Update(MicroHIDParticles parts, FiringModeControllerModule firingMode, float scale)
		{
			ParticleSystem.TrailModule trails = this._particleSys.trails;
			trails.widthOverTrail = parts._targetTrailWidth * scale;
			float targetLength;
			if (this._viewmodelMode)
			{
				targetLength = firingMode.FiringRange;
			}
			else
			{
				this.GetTransformationData(parts._camera, firingMode.FiringRange, out targetLength, out var targetFwd);
				this._particleSys.transform.forward = targetFwd;
			}
			ParticleSystem.MainModule main = this._particleSys.main;
			main.startLifetime = targetLength * parts._rangeToLifetimeRatio * scale;
		}

		private readonly void GetTransformationData(Transform camera, float firingRange, out float targetLength, out Vector3 targetFwd)
		{
			Vector3 position = camera.position;
			Vector3 forward = camera.forward;
			Transform transform = this._particleSys.transform;
			targetFwd = forward;
			targetLength = 0f;
			RaycastHit hitInfo;
			Vector3 vector = (Physics.Raycast(position, forward, out hitInfo, firingRange, MicroHIDParticles.VisualizedCollisionMask) ? hitInfo.point : (position + forward * firingRange)) - transform.position;
			float magnitude = vector.magnitude;
			if (magnitude != 0f)
			{
				targetFwd = vector / magnitude;
				if (!(Vector3.Dot(targetFwd, forward) < 0.6f))
				{
					targetLength = magnitude;
				}
			}
		}
	}

	private static readonly CachedLayerMask VisualizedCollisionMask = new CachedLayerMask("Default", "Glass", "Door");

	[SerializeField]
	private GameObject _allParticles;

	[SerializeField]
	private float _rangeToLifetimeRatio;

	[SerializeField]
	private float _targetTrailWidth;

	[SerializeField]
	private ParticleSystem _breakParticles;

	[SerializeField]
	private AimedLaser[] _lasers;

	[SerializeField]
	private GameObject _chargeFireExtras;

	[SerializeField]
	private ParticleSystem[] _startFiringParticles;

	[SerializeField]
	private WindupArc[] _windupArcs;

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

	private bool WasFiring
	{
		get
		{
			if (!this._cycle.TryGetElapsed(MicroHidPhase.WindingUp, out var elapsed))
			{
				return false;
			}
			if (!this._cycle.TryGetElapsed(MicroHidPhase.Firing, out var elapsed2))
			{
				return false;
			}
			return elapsed > elapsed2;
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
		if (this._initalized)
		{
			this.UpdateAll();
			this._prevPhase = this._cycle.Phase;
		}
	}

	private void UpdateAll()
	{
		bool flag = this._cycle.Phase != MicroHidPhase.Standby;
		this._allParticles.SetActive(flag);
		if (!flag)
		{
			this._wasBroken = false;
			return;
		}
		this.UpdateBroken();
		this.UpdateFiring();
		float num = (this._wasBroken ? 0f : WindupSyncModule.GetProgress(this._serial));
		ParticleSystem.MainModule main = this._windupBlinkerSystem.main;
		main.startColor = this._windupBlinkerGradient.Evaluate(num);
		WindupArc[] windupArcs = this._windupArcs;
		foreach (WindupArc windupArc in windupArcs)
		{
			windupArc.Update(num);
		}
	}

	private void UpdateFiring()
	{
		bool num = this._cycle.Phase == MicroHidPhase.Firing;
		MicroHidPhase prevPhase = this._prevPhase;
		bool flag = prevPhase == MicroHidPhase.WindingUp || prevPhase == MicroHidPhase.WoundUpSustain;
		bool num2 = num && flag;
		float currentPhaseElapsed = this._cycle.CurrentPhaseElapsed;
		if (num2)
		{
			ParticleSystem[] startFiringParticles = this._startFiringParticles;
			for (int i = 0; i < startFiringParticles.Length; i++)
			{
				startFiringParticles[i].Play(withChildren: false);
			}
		}
		prevPhase = this._cycle.Phase;
		float num3;
		if (prevPhase != MicroHidPhase.WindingDown)
		{
			if (prevPhase != MicroHidPhase.Firing)
			{
				goto IL_00a7;
			}
			num3 = Mathf.Clamp01(currentPhaseElapsed / this._arcGrowTime);
		}
		else
		{
			if (!this.WasFiring)
			{
				goto IL_00a7;
			}
			num3 = Mathf.Clamp01(1f - currentPhaseElapsed / this._arcGrowTime);
		}
		goto IL_00ad;
		IL_00a7:
		num3 = 0f;
		goto IL_00ad;
		IL_00ad:
		AimedLaser[] lasers;
		if (num3 == 0f || !this._cycle.TryGetLastFiringController(out var ret))
		{
			this._chargeFireExtras.SetActive(value: false);
			lasers = this._lasers;
			foreach (AimedLaser aimedLaser in lasers)
			{
				aimedLaser.SetActive(b: false);
			}
			return;
		}
		this._chargeFireExtras.SetActive(ret is ChargeFireModeModule);
		this._chargeFireExtras.transform.localScale = Vector3.up * num3;
		lasers = this._lasers;
		for (int i = 0; i < lasers.Length; i++)
		{
			AimedLaser aimedLaser2 = lasers[i];
			aimedLaser2.SetActive(b: true);
			aimedLaser2.Update(this, ret, num3);
		}
	}

	private void UpdateBroken()
	{
		if (this._wasBroken || !BrokenSyncModule.TryGetBrokenElapsed(this._serial, out var elapsed))
		{
			return;
		}
		if (elapsed < 5f)
		{
			this._breakParticles.Simulate(elapsed, withChildren: true, restart: true);
			if (this._breakParticles.isPaused)
			{
				this._breakParticles.Play();
			}
		}
		this._wasBroken = true;
	}
}

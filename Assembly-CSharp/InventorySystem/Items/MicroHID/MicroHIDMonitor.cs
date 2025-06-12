using System;
using System.Collections.Generic;
using System.Text;
using InventorySystem.Items.MicroHID.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace InventorySystem.Items.MicroHID;

public class MicroHIDMonitor : MonoBehaviour
{
	[Serializable]
	private class LcdElementInstance
	{
		private bool? _prevActive;

		private float? _prevAlpha;

		private float _remainingBlinking;

		private const float DisabledAlpha = 0.22f;

		[SerializeField]
		private Graphic _target;

		public bool Active;

		public bool Blinking;

		public float BlinkFrequency;

		public float BlinkRatio;

		public float BlinkDuration;

		public float DisabledAlphaMultiplier;

		[field: SerializeField]
		public LcdElementType Type { get; private set; }

		public double BlinkOffset { get; private set; }

		public void Update()
		{
			bool flag = this.Active;
			if (flag && this.Blinking)
			{
				this._remainingBlinking -= Time.deltaTime;
				flag = this._remainingBlinking <= 0f || MicroHIDMonitor.GetBlinkActive(this.BlinkFrequency, this.BlinkRatio, this.BlinkOffset);
			}
			float num = (flag ? 1f : (0.22f * this.DisabledAlphaMultiplier));
			if (this._prevAlpha != num)
			{
				this._prevAlpha = num;
				Color color = this._target.color;
				color.a = num;
				this._target.color = color;
			}
			if (this._prevActive != this.Active)
			{
				this._prevActive = this.Active;
				if (this.Active)
				{
					this.BlinkOffset = Time.timeAsDouble;
					this._remainingBlinking = ((this.BlinkDuration < 0f) ? float.MaxValue : this.BlinkDuration);
				}
			}
		}
	}

	private enum LcdElementType
	{
		Windup,
		Winddown,
		ReadyToFire,
		Firing,
		Standby,
		Broken,
		JeffHappy,
		JeffSad,
		JeffConcerned
	}

	[SerializeField]
	private LcdElementInstance[] _lcdElementInstances;

	[SerializeField]
	private GameObject[] _batteryIndicators;

	[SerializeField]
	private TMP_Text _windupProgress;

	[SerializeField]
	[FormerlySerializedAs("_remainigCharge")]
	private TMP_Text _remainingCharge;

	[SerializeField]
	private char _windupChar;

	[SerializeField]
	private int _maxWindupLen;

	[SerializeField]
	private Image _jeffRenderer;

	[SerializeField]
	private Sprite _jeffHappy;

	[SerializeField]
	private Sprite _jeffSad;

	[SerializeField]
	private Sprite _jeffConcerned;

	[SerializeField]
	private Sprite _jeffDead;

	private ushort _serial;

	private bool _worldmodelMode;

	private IIdentifierProvider _identifierProvider;

	private CycleController _cycle;

	private int _lastEnergyMultiplied;

	private readonly StringBuilder _sharedSb = new StringBuilder();

	private readonly Dictionary<LcdElementType, LcdElementInstance> _lcdElements = new Dictionary<LcdElementType, LcdElementInstance>();

	private bool BlinkNoBattery => MicroHIDMonitor.GetBlinkActive(1.2f, 0.4f);

	private void Start()
	{
		if (!base.transform.TryGetComponentInParent<IIdentifierProvider>(out this._identifierProvider))
		{
			return;
		}
		if (this._identifierProvider is MicroHIDViewmodel)
		{
			this._worldmodelMode = false;
			this._lcdElementInstances.ForEach(delegate(LcdElementInstance x)
			{
				this._lcdElements.Add(x.Type, x);
			});
		}
		else
		{
			this._worldmodelMode = true;
		}
	}

	private void OnDisable()
	{
		this._serial = 0;
	}

	private void Update()
	{
		if (this._serial != this._identifierProvider.ItemId.SerialNumber)
		{
			this._serial = this._identifierProvider.ItemId.SerialNumber;
			this._cycle = CycleSyncModule.GetCycleController(this._serial);
		}
		this.UpdateBattery();
		if (!this._worldmodelMode)
		{
			this.UpdateWindup();
			this.UpdatePhase();
			this.UpdateJeff();
			LcdElementInstance[] lcdElementInstances = this._lcdElementInstances;
			for (int i = 0; i < lcdElementInstances.Length; i++)
			{
				lcdElementInstances[i].Update();
			}
		}
	}

	private void UpdateBattery()
	{
		float energy = EnergyManagerModule.GetEnergy(this._serial);
		int num = Mathf.CeilToInt(energy * (float)this._batteryIndicators.Length);
		this._remainingCharge.enabled = num > 0 || this.BlinkNoBattery;
		int num2 = Mathf.CeilToInt(energy * 100f);
		if (num2 != this._lastEnergyMultiplied)
		{
			this._remainingCharge.text = num2.ToString();
			this._lastEnergyMultiplied = num2;
		}
		bool flag = num > 1 || this.BlinkNoBattery || this._cycle.Phase == MicroHidPhase.Firing;
		for (int i = 0; i < this._batteryIndicators.Length; i++)
		{
			this._batteryIndicators[i].SetActive(i < num && flag);
		}
	}

	private void UpdateWindup()
	{
		int num = Mathf.CeilToInt(WindupSyncModule.GetProgress(this._serial) * (float)this._maxWindupLen);
		this._sharedSb.Clear();
		for (int i = 0; i < num; i++)
		{
			this._sharedSb.Append(this._windupChar);
		}
		LcdElementInstance lcdElementInstance = this._lcdElements[LcdElementType.ReadyToFire];
		bool blinkActive = MicroHIDMonitor.GetBlinkActive(lcdElementInstance.BlinkFrequency, lcdElementInstance.BlinkRatio, lcdElementInstance.BlinkOffset);
		this._windupProgress.text = this._sharedSb.ToString();
		this._windupProgress.enabled = this._cycle.Phase != MicroHidPhase.WoundUpSustain || blinkActive;
	}

	private void UpdatePhase()
	{
		bool broken = BrokenSyncModule.GetBroken(this._serial);
		this._lcdElements[LcdElementType.Broken].Active = broken;
		this._lcdElementInstances.ForEach(UpdatePhaseLcd);
	}

	private void UpdatePhaseLcd(LcdElementInstance inst)
	{
		switch (inst.Type)
		{
		case LcdElementType.Windup:
			inst.Active = this._cycle.Phase == MicroHidPhase.WindingUp;
			break;
		case LcdElementType.Winddown:
			inst.Active = this._cycle.Phase == MicroHidPhase.WindingDown;
			break;
		case LcdElementType.ReadyToFire:
			inst.Active = this._cycle.Phase == MicroHidPhase.WoundUpSustain;
			break;
		case LcdElementType.Firing:
			inst.Active = this._cycle.Phase == MicroHidPhase.Firing;
			break;
		case LcdElementType.Standby:
			inst.Active = this._cycle.Phase == MicroHidPhase.Standby;
			break;
		}
	}

	private void UpdateJeff()
	{
		if (EnergyManagerModule.GetEnergy(this._serial) == 0f)
		{
			this._jeffRenderer.sprite = this._jeffDead;
			return;
		}
		if (BrokenSyncModule.GetBroken(this._serial))
		{
			this._jeffRenderer.sprite = this._jeffSad;
			return;
		}
		float num = 8f;
		bool flag = this._cycle.Phase == MicroHidPhase.WoundUpSustain && this._cycle.CurrentPhaseElapsed > num;
		this._jeffRenderer.sprite = (flag ? this._jeffConcerned : this._jeffHappy);
	}

	private static bool GetBlinkActive(float frequency, float ratio, double offset = 0.0)
	{
		double num = (Time.timeAsDouble - offset) * (double)frequency;
		return (float)(num - (double)(int)num) <= ratio;
	}
}

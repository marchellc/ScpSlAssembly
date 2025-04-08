using System;
using System.Collections.Generic;
using System.Text;
using InventorySystem.Items.MicroHID.Modules;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDMonitor : MonoBehaviour
	{
		private bool BlinkNoBattery
		{
			get
			{
				return MicroHIDMonitor.GetBlinkActive(1.2f, 0.4f, 0.0);
			}
		}

		private void Start()
		{
			if (!base.transform.TryGetComponentInParent(out this._identifierProvider))
			{
				return;
			}
			if (this._identifierProvider is MicroHIDViewmodel)
			{
				this._worldmodelMode = false;
				this._lcdElementInstances.ForEach(delegate(MicroHIDMonitor.LcdElementInstance x)
				{
					this._lcdElements.Add(x.Type, x);
				});
				return;
			}
			this._worldmodelMode = true;
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
			if (this._worldmodelMode)
			{
				return;
			}
			this.UpdateWindup();
			this.UpdatePhase();
			this.UpdateJeff();
			MicroHIDMonitor.LcdElementInstance[] lcdElementInstances = this._lcdElementInstances;
			for (int i = 0; i < lcdElementInstances.Length; i++)
			{
				lcdElementInstances[i].Update();
			}
		}

		private void UpdateBattery()
		{
			float energy = EnergyManagerModule.GetEnergy(this._serial);
			int num = Mathf.CeilToInt(energy * (float)this._batteryIndicators.Length);
			this._remainigCharge.enabled = num > 0 || this.BlinkNoBattery;
			this._remainigCharge.text = Mathf.CeilToInt(energy * 100f).ToString();
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
			MicroHIDMonitor.LcdElementInstance lcdElementInstance = this._lcdElements[MicroHIDMonitor.LcdElementType.ReadyToFire];
			bool blinkActive = MicroHIDMonitor.GetBlinkActive(lcdElementInstance.BlinkFrequency, lcdElementInstance.BlinkRatio, lcdElementInstance.BlinkOffset);
			this._windupProgress.text = this._sharedSb.ToString();
			this._windupProgress.enabled = this._cycle.Phase != MicroHidPhase.WoundUpSustain || blinkActive;
		}

		private void UpdatePhase()
		{
			bool broken = BrokenSyncModule.GetBroken(this._serial);
			this._lcdElements[MicroHIDMonitor.LcdElementType.Broken].Active = broken;
			this._lcdElementInstances.ForEach(new Action<MicroHIDMonitor.LcdElementInstance>(this.UpdatePhaseLcd));
		}

		private void UpdatePhaseLcd(MicroHIDMonitor.LcdElementInstance inst)
		{
			switch (inst.Type)
			{
			case MicroHIDMonitor.LcdElementType.Windup:
				inst.Active = this._cycle.Phase == MicroHidPhase.WindingUp;
				return;
			case MicroHIDMonitor.LcdElementType.Winddown:
				inst.Active = this._cycle.Phase == MicroHidPhase.WindingDown;
				return;
			case MicroHIDMonitor.LcdElementType.ReadyToFire:
				inst.Active = this._cycle.Phase == MicroHidPhase.WoundUpSustain;
				return;
			case MicroHIDMonitor.LcdElementType.Firing:
				inst.Active = this._cycle.Phase == MicroHidPhase.Firing;
				return;
			case MicroHIDMonitor.LcdElementType.Standby:
				inst.Active = this._cycle.Phase == MicroHidPhase.Standby;
				return;
			default:
				return;
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
			float num = 13f;
			bool flag = this._cycle.Phase == MicroHidPhase.WoundUpSustain && this._cycle.CurrentPhaseElapsed > num;
			this._jeffRenderer.sprite = (flag ? this._jeffConcerned : this._jeffHappy);
		}

		private static bool GetBlinkActive(float frequency, float ratio, double offset = 0.0)
		{
			double num = (Time.timeAsDouble - offset) * (double)frequency;
			return (float)(num - (double)((int)num)) <= ratio;
		}

		[SerializeField]
		private MicroHIDMonitor.LcdElementInstance[] _lcdElementInstances;

		[SerializeField]
		private GameObject[] _batteryIndicators;

		[SerializeField]
		private TMP_Text _windupProgress;

		[SerializeField]
		private TMP_Text _remainigCharge;

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

		private readonly StringBuilder _sharedSb = new StringBuilder();

		private readonly Dictionary<MicroHIDMonitor.LcdElementType, MicroHIDMonitor.LcdElementInstance> _lcdElements = new Dictionary<MicroHIDMonitor.LcdElementType, MicroHIDMonitor.LcdElementInstance>();

		[Serializable]
		private class LcdElementInstance
		{
			public MicroHIDMonitor.LcdElementType Type { get; private set; }

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
				float? prevAlpha = this._prevAlpha;
				float num2 = num;
				if (!((prevAlpha.GetValueOrDefault() == num2) & (prevAlpha != null)))
				{
					this._prevAlpha = new float?(num);
					Color color = this._target.color;
					color.a = num;
					this._target.color = color;
				}
				bool? prevActive = this._prevActive;
				bool active = this.Active;
				if (!((prevActive.GetValueOrDefault() == active) & (prevActive != null)))
				{
					this._prevActive = new bool?(this.Active);
					if (!this.Active)
					{
						return;
					}
					this.BlinkOffset = Time.timeAsDouble;
					this._remainingBlinking = ((this.BlinkDuration < 0f) ? float.MaxValue : this.BlinkDuration);
				}
			}

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
	}
}

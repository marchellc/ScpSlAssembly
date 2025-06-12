using System;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class SubsequentShotsCounter
{
	private bool _destructed;

	private float _counter;

	private float _decaySpeed;

	private float _remainingSustain;

	private readonly Firearm _firearm;

	private readonly float _sustainMultiplier;

	private readonly float _sustainAddition;

	private readonly float _decayTime;

	public int SubsequentShots
	{
		get
		{
			return Mathf.CeilToInt(this._counter);
		}
		private set
		{
			this._counter = value;
		}
	}

	public event Action OnShotRecorded;

	public event Action OnReset;

	private void OnShot(ShotEvent ev)
	{
		if (ev.ItemId.SerialNumber == this._firearm.ItemSerial)
		{
			this.SubsequentShots++;
			IActionModule module;
			float num = (this._firearm.TryGetModule<IActionModule>(out module) ? (1f / module.DisplayCyclicRate) : 0f);
			this._remainingSustain = num * this._sustainMultiplier + this._sustainAddition;
			if (this._decayTime > 0f)
			{
				this._decaySpeed = this._counter / this._decayTime;
			}
			this.OnShotRecorded?.Invoke();
		}
	}

	public void Update()
	{
		if (this._remainingSustain > 0f)
		{
			this._remainingSustain -= Time.deltaTime;
		}
		else
		{
			if (this.SubsequentShots == 0)
			{
				return;
			}
			if (this._decayTime > 0f)
			{
				this._counter -= this._decaySpeed * Time.deltaTime;
				if (this._counter > 0f)
				{
					return;
				}
			}
			this._counter = 0f;
			this.OnReset?.Invoke();
		}
	}

	public void Destruct()
	{
		if (!this._destructed)
		{
			this._destructed = true;
			ShotEventManager.OnShot -= OnShot;
		}
	}

	public SubsequentShotsCounter(Firearm firearm, float sustainCycleTimeMultiplier = 1f, float sustainAdditionSeconds = 0.1f, float decayTimeSeconds = 0.4f)
	{
		this._firearm = firearm;
		this._sustainMultiplier = sustainCycleTimeMultiplier;
		this._sustainAddition = sustainAdditionSeconds;
		this._decayTime = decayTimeSeconds;
		ShotEventManager.OnShot += OnShot;
	}

	~SubsequentShotsCounter()
	{
		this.Destruct();
	}
}

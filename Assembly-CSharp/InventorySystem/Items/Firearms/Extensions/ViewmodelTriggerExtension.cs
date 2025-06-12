using AudioPooling;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.Modules.Misc;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class ViewmodelTriggerExtension : MonoBehaviour, IViewmodelExtension
{
	private AnimatedFirearmViewmodel _viewmodel;

	private Firearm _firearm;

	private float _lastShotTime;

	private float _weight;

	private bool _released;

	private const float DefaultPullSpeed = 15f;

	private const float DefaultReleaseSpeed = 6.5f;

	[Range(0f, 1f)]
	[SerializeField]
	private float _triggerVisualReleaseRate;

	[SerializeField]
	private float _pullSpeed = 15f;

	[SerializeField]
	private float _releaseSpeed = 6.5f;

	[SerializeField]
	private AnimatorLayerMask _triggerOverrideLayer;

	[SerializeField]
	private AudioClip _triggerSound;

	private bool TriggerHeld
	{
		get
		{
			if (!this._firearm.TryGetModule<ITriggerControllerModule>(out var module) || !module.TriggerHeld)
			{
				return false;
			}
			if (!this._firearm.TryGetModule<IActionModule>(out var module2))
			{
				return true;
			}
			float num = 1f / module2.DisplayCyclicRate;
			float num2 = (1f - this._triggerVisualReleaseRate) * num;
			double num3 = Time.timeSinceLevelLoad - this._lastShotTime;
			if (!(num3 < (double)num2))
			{
				return num3 > (double)(num + Time.deltaTime);
			}
			return true;
		}
	}

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._viewmodel = viewmodel;
		this._firearm = viewmodel.ParentFirearm;
	}

	private void Awake()
	{
		ShotEventManager.OnShot += OnShot;
	}

	private void OnDestroy()
	{
		ShotEventManager.OnShot -= OnShot;
	}

	private void OnShot(ShotEvent ev)
	{
		if (!(this._viewmodel.ItemId != ev.ItemId))
		{
			this._lastShotTime = Time.timeSinceLevelLoad;
		}
	}

	private void Update()
	{
		if (this.TriggerHeld)
		{
			if (this._released)
			{
				AudioSourcePoolManager.Play2D(this._triggerSound);
				this._released = false;
			}
			this._weight += Time.deltaTime * this._pullSpeed;
		}
		else
		{
			this._weight -= Time.deltaTime * this._releaseSpeed;
			this._released = true;
		}
		this._weight = Mathf.Clamp01(this._weight);
		this._triggerOverrideLayer.SetWeight(this._viewmodel.AnimatorSetLayerWeight, this._weight);
	}
}

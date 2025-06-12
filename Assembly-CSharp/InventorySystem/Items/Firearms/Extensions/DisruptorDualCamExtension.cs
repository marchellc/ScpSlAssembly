using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using MEC;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class DisruptorDualCamExtension : ViewmodelDualCamExtension
{
	[Serializable]
	private struct IconDefinition
	{
		public IconType Type;

		public GameObject Root;
	}

	private enum IconType
	{
		None,
		DesintegratorMode,
		BurstMode,
		CriticalWarn,
		CrashWarn
	}

	[SerializeField]
	private Transform _root;

	[SerializeField]
	private IconDefinition[] _icons;

	private IconType _selectedIcon;

	private CoroutineHandle _lastHandle;

	private ItemIdentifier _itemId;

	private MagazineModule _magModule;

	private DisruptorModeSelector _selectorModule;

	public override void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		base.InitViewmodel(viewmodel);
		this._itemId = viewmodel.ItemId;
		if (viewmodel.ParentFirearm.TryGetModules<MagazineModule, DisruptorModeSelector>(out this._magModule, out this._selectorModule))
		{
			ShotEventManager.OnShot += OnShot;
			this._selectorModule.OnAnimationRequested += OnAnimationRequested;
		}
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		IconDefinition[] icons = this._icons;
		for (int i = 0; i < icons.Length; i++)
		{
			IconDefinition iconDefinition = icons[i];
			iconDefinition.Root.SetActive(iconDefinition.Type == this._selectedIcon);
		}
		this._root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
	}

	private void OnDestroy()
	{
		ShotEventManager.OnShot -= OnShot;
		if (this._selectorModule != null)
		{
			this._selectorModule.OnAnimationRequested -= OnAnimationRequested;
		}
	}

	private void OnDisable()
	{
		this._selectedIcon = IconType.None;
	}

	private void SetIcon(IconType newIcon)
	{
		this._selectedIcon = newIcon;
	}

	private void PlayAnimation(IconType icon)
	{
		switch (icon)
		{
		case IconType.None:
			this.SetIcon(icon);
			break;
		case IconType.DesintegratorMode:
			this.RunCoroutine(this.AnimateDisintegratorMode());
			break;
		case IconType.BurstMode:
			this.RunCoroutine(this.AnimateBurstMode());
			break;
		case IconType.CriticalWarn:
			this.RunCoroutine(this.AnimateCriticalWarn());
			break;
		case IconType.CrashWarn:
			this.RunCoroutine(this.AnimateCrash());
			break;
		}
	}

	private void RunCoroutine(IEnumerator<float> coroutine)
	{
		if (this._lastHandle.IsRunning)
		{
			Timing.KillCoroutines(this._lastHandle);
			this.SetIcon(IconType.None);
		}
		this._lastHandle = Timing.RunCoroutine(coroutine.CancelWith(base.gameObject), Segment.Update);
	}

	private IEnumerator<float> AnimateDisintegratorMode()
	{
		this.SetIcon(IconType.DesintegratorMode);
		yield return Timing.WaitForSeconds(0.4f);
		this.SetIcon(IconType.None);
	}

	private IEnumerator<float> AnimateBurstMode()
	{
		for (int i = 0; i < 3; i++)
		{
			this.SetIcon(IconType.BurstMode);
			yield return Timing.WaitForSeconds(0.082f);
			this.SetIcon(IconType.None);
			yield return Timing.WaitForSeconds(0.035f);
		}
	}

	private IEnumerator<float> AnimateCriticalWarn()
	{
		for (int i = 0; i < 3; i++)
		{
			this.SetIcon(IconType.CriticalWarn);
			yield return Timing.WaitForSeconds(0.082f);
			this.SetIcon(IconType.None);
			yield return Timing.WaitForSeconds(0.055f);
		}
	}

	private IEnumerator<float> AnimateCrash()
	{
		for (int i = 0; i < 20; i++)
		{
			this.SetIcon(IconType.CriticalWarn);
			yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.07f));
			this.SetIcon(IconType.CrashWarn);
			yield return Timing.WaitForSeconds(UnityEngine.Random.Range(0.12f, 0.2f));
			if (UnityEngine.Random.value > 0.6f)
			{
				this.SetIcon(IconType.None);
				yield return Timing.WaitForSeconds(0.05f);
			}
		}
	}

	private void OnShot(ShotEvent shotEvent)
	{
		if (!(shotEvent.ItemId != this._itemId))
		{
			switch (this._magModule.AmmoStored)
			{
			case 0:
				this.PlayAnimation(IconType.CrashWarn);
				break;
			case 1:
				this.PlayAnimation(IconType.CriticalWarn);
				break;
			}
		}
	}

	private void OnAnimationRequested()
	{
		this.PlayAnimation(this._selectorModule.SingleShotSelected ? IconType.DesintegratorMode : IconType.BurstMode);
	}
}

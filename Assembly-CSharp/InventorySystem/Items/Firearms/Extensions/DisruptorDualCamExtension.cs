using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using MEC;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class DisruptorDualCamExtension : ViewmodelDualCamExtension
	{
		public override void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			base.InitViewmodel(viewmodel);
			this._itemId = viewmodel.ItemId;
			if (!viewmodel.ParentFirearm.TryGetModules(out this._magModule, out this._selectorModule))
			{
				return;
			}
			ShotEventManager.OnShot += this.OnShot;
			this._selectorModule.OnAnimationRequested += this.OnAnimationRequested;
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			foreach (DisruptorDualCamExtension.IconDefinition iconDefinition in this._icons)
			{
				iconDefinition.Root.SetActive(iconDefinition.Type == this._selectedIcon);
			}
			this._root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
		}

		private void OnDestroy()
		{
			ShotEventManager.OnShot -= this.OnShot;
			if (this._selectorModule != null)
			{
				this._selectorModule.OnAnimationRequested -= this.OnAnimationRequested;
			}
		}

		private void OnDisable()
		{
			this._selectedIcon = DisruptorDualCamExtension.IconType.None;
		}

		private void SetIcon(DisruptorDualCamExtension.IconType newIcon)
		{
			this._selectedIcon = newIcon;
		}

		private void PlayAnimation(DisruptorDualCamExtension.IconType icon)
		{
			switch (icon)
			{
			case DisruptorDualCamExtension.IconType.None:
				this.SetIcon(icon);
				return;
			case DisruptorDualCamExtension.IconType.DesintegratorMode:
				this.RunCoroutine(this.AnimateDisintegratorMode());
				return;
			case DisruptorDualCamExtension.IconType.BurstMode:
				this.RunCoroutine(this.AnimateBurstMode());
				return;
			case DisruptorDualCamExtension.IconType.CriticalWarn:
				this.RunCoroutine(this.AnimateCriticalWarn());
				return;
			case DisruptorDualCamExtension.IconType.CrashWarn:
				this.RunCoroutine(this.AnimateCrash());
				return;
			default:
				return;
			}
		}

		private void RunCoroutine(IEnumerator<float> coroutine)
		{
			if (this._lastHandle.IsRunning)
			{
				Timing.KillCoroutines(new CoroutineHandle[] { this._lastHandle });
				this.SetIcon(DisruptorDualCamExtension.IconType.None);
			}
			this._lastHandle = Timing.RunCoroutine(coroutine.CancelWith(base.gameObject), Segment.Update);
		}

		private IEnumerator<float> AnimateDisintegratorMode()
		{
			this.SetIcon(DisruptorDualCamExtension.IconType.DesintegratorMode);
			yield return Timing.WaitForSeconds(0.4f);
			this.SetIcon(DisruptorDualCamExtension.IconType.None);
			yield break;
		}

		private IEnumerator<float> AnimateBurstMode()
		{
			int num;
			for (int i = 0; i < 3; i = num + 1)
			{
				this.SetIcon(DisruptorDualCamExtension.IconType.BurstMode);
				yield return Timing.WaitForSeconds(0.082f);
				this.SetIcon(DisruptorDualCamExtension.IconType.None);
				yield return Timing.WaitForSeconds(0.035f);
				num = i;
			}
			yield break;
		}

		private IEnumerator<float> AnimateCriticalWarn()
		{
			int num;
			for (int i = 0; i < 3; i = num + 1)
			{
				this.SetIcon(DisruptorDualCamExtension.IconType.CriticalWarn);
				yield return Timing.WaitForSeconds(0.082f);
				this.SetIcon(DisruptorDualCamExtension.IconType.None);
				yield return Timing.WaitForSeconds(0.055f);
				num = i;
			}
			yield break;
		}

		private IEnumerator<float> AnimateCrash()
		{
			int num;
			for (int i = 0; i < 20; i = num + 1)
			{
				this.SetIcon(DisruptorDualCamExtension.IconType.CriticalWarn);
				yield return Timing.WaitForSeconds(global::UnityEngine.Random.Range(0.03f, 0.07f));
				this.SetIcon(DisruptorDualCamExtension.IconType.CrashWarn);
				yield return Timing.WaitForSeconds(global::UnityEngine.Random.Range(0.12f, 0.2f));
				if (global::UnityEngine.Random.value > 0.6f)
				{
					this.SetIcon(DisruptorDualCamExtension.IconType.None);
					yield return Timing.WaitForSeconds(0.05f);
				}
				num = i;
			}
			yield break;
		}

		private void OnShot(ShotEvent shotEvent)
		{
			if (shotEvent.ItemId != this._itemId)
			{
				return;
			}
			int ammoStored = this._magModule.AmmoStored;
			if (ammoStored == 0)
			{
				this.PlayAnimation(DisruptorDualCamExtension.IconType.CrashWarn);
				return;
			}
			if (ammoStored != 1)
			{
				return;
			}
			this.PlayAnimation(DisruptorDualCamExtension.IconType.CriticalWarn);
		}

		private void OnAnimationRequested()
		{
			this.PlayAnimation(this._selectorModule.SingleShotSelected ? DisruptorDualCamExtension.IconType.DesintegratorMode : DisruptorDualCamExtension.IconType.BurstMode);
		}

		[SerializeField]
		private Transform _root;

		[SerializeField]
		private DisruptorDualCamExtension.IconDefinition[] _icons;

		private DisruptorDualCamExtension.IconType _selectedIcon;

		private CoroutineHandle _lastHandle;

		private ItemIdentifier _itemId;

		private MagazineModule _magModule;

		private DisruptorModeSelector _selectorModule;

		[Serializable]
		private struct IconDefinition
		{
			public DisruptorDualCamExtension.IconType Type;

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
	}
}

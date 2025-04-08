using System;
using System.Diagnostics;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class MagazineRoundsViewmodelExtension : MonoBehaviour, IViewmodelExtension
	{
		private int DisplayedAmount
		{
			get
			{
				int num = IDisplayableAmmoProviderModule.GetCombinedDisplayAmmo(this._fa).Magazines;
				if (this._onShotSw.IsRunning && this._onShotSw.Elapsed.TotalSeconds < (double)this._onShotSustain)
				{
					num += this._onShotOffset;
				}
				return num;
			}
		}

		public void RemoveOffsets()
		{
			this._onShotSw.Reset();
			this.SetAmmo(this.DisplayedAmount);
		}

		public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this._fa = viewmodel.ParentFirearm;
			this._itemId = viewmodel.ItemId;
		}

		private void OnShot(ShotEvent shotEvent)
		{
			if (shotEvent.ItemId != this._itemId)
			{
				return;
			}
			this._onShotSw.Restart();
		}

		private void Awake()
		{
			ShotEventManager.OnShot += this.OnShot;
		}

		private void OnDestroy()
		{
			ShotEventManager.OnShot -= this.OnShot;
		}

		private void Update()
		{
			IReloaderModule reloaderModule;
			if (!this._fa.TryGetModule(out reloaderModule, true))
			{
				return;
			}
			if (!reloaderModule.IsReloadingOrUnloading)
			{
				this.SetAmmo(this.DisplayedAmount);
				this._magRefilled = false;
				return;
			}
			Vector3 vector;
			if (!ViewmodelCamera.TryGetViewportPoint(this._visibilityBeacon.position, out vector))
			{
				return;
			}
			if (!this._magRefilled && vector.y > 0f)
			{
				return;
			}
			IPrimaryAmmoContainerModule primaryAmmoContainerModule;
			if (!this._fa.TryGetModule(out primaryAmmoContainerModule, true))
			{
				return;
			}
			int num;
			if (!ReserveAmmoSync.TryGet(this._fa.Owner, primaryAmmoContainerModule.AmmoType, out num))
			{
				return;
			}
			this.SetAmmo(num);
		}

		private void SetAmmo(int ammo)
		{
			for (int i = 0; i < this._rounds.Length; i++)
			{
				this._rounds[i].SetActive(i < ammo);
			}
		}

		[SerializeField]
		private GameObject[] _rounds;

		[SerializeField]
		private Transform _visibilityBeacon;

		[SerializeField]
		private int _onShotOffset;

		[SerializeField]
		private float _onShotSustain;

		private Firearm _fa;

		private ItemIdentifier _itemId;

		private bool _magRefilled;

		private readonly Stopwatch _onShotSw = new Stopwatch();
	}
}

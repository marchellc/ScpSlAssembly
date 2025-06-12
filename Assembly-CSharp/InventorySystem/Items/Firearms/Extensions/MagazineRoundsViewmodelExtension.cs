using System.Diagnostics;
using InventorySystem.Items.Firearms.Ammo;
using InventorySystem.Items.Firearms.Modules;
using InventorySystem.Items.Firearms.ShotEvents;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class MagazineRoundsViewmodelExtension : MonoBehaviour, IViewmodelExtension
{
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
		if (!(shotEvent.ItemId != this._itemId))
		{
			this._onShotSw.Restart();
		}
	}

	private void Awake()
	{
		ShotEventManager.OnShot += OnShot;
	}

	private void OnDestroy()
	{
		ShotEventManager.OnShot -= OnShot;
	}

	private void Update()
	{
		if (this._fa.TryGetModule<IReloaderModule>(out var module))
		{
			Vector3 viewport;
			IPrimaryAmmoContainerModule module2;
			int reserveAmmo;
			if (!module.IsReloadingOrUnloading)
			{
				this.SetAmmo(this.DisplayedAmount);
				this._magRefilled = false;
			}
			else if (ViewmodelCamera.TryGetViewportPoint(this._visibilityBeacon.position, out viewport) && (this._magRefilled || !(viewport.y > 0f)) && this._fa.TryGetModule<IPrimaryAmmoContainerModule>(out module2) && ReserveAmmoSync.TryGet(this._fa.Owner, module2.AmmoType, out reserveAmmo))
			{
				this.SetAmmo(reserveAmmo);
			}
		}
	}

	private void SetAmmo(int ammo)
	{
		for (int i = 0; i < this._rounds.Length; i++)
		{
			this._rounds[i].SetActive(i < ammo);
		}
	}
}

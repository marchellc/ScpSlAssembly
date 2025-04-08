using System;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Ammo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.GUI
{
	[Serializable]
	public class AmmoElement : MonoBehaviour
	{
		public void UseButton(int type)
		{
			if (!this._allowDropping)
			{
				return;
			}
			int num = ((type == 0) ? this._lowAmount : ((type == 1) ? this._medAmount : this._highAmount));
			ReferenceHub.LocalHub.inventory.CmdDropAmmo((byte)this._targetItem.ItemTypeId, (ushort)num);
		}

		public void Setup(ItemType type, Color classColor)
		{
			if (!InventoryItemLoader.AvailableItems.TryGetValue(type, out this._targetItem))
			{
				throw new InvalidOperationException("Item " + type.ToString() + " is not defined. Cannot create an ammo element for it.");
			}
			this._iconImg.texture = this._targetItem.Icon;
			TMP_Text nameTxt = this._nameTxt;
			IItemNametag itemNametag = this._targetItem as IItemNametag;
			nameTxt.text = ((itemNametag != null) ? itemNametag.Name : type.ToString());
			Graphic[] paintableParts = this._paintableParts;
			for (int i = 0; i < paintableParts.Length; i++)
			{
				paintableParts[i].color = classColor;
			}
		}

		public void UpdateAmount(int amount)
		{
			if (amount == 0)
			{
				base.gameObject.SetActive(false);
				return;
			}
			base.gameObject.SetActive(true);
			this._amountIndicator.text = amount.ToString();
			AmmoItem ammoItem = this._targetItem as AmmoItem;
			if (ammoItem != null)
			{
				AmmoPickup ammoPickup = ammoItem.PickupDropModel as AmmoPickup;
				if (ammoPickup != null)
				{
					this._medAmount = (int)ammoPickup.SavedAmmo;
					this._highAmount = this._medAmount * 2;
					this._lowAmount = (((float)this._medAmount % 2f == 0f) ? (this._medAmount / 2) : (this._medAmount * 2 / 3));
					goto IL_00C3;
				}
			}
			this._highAmount = amount;
			this._medAmount = Mathf.FloorToInt((float)amount / 2f);
			this._lowAmount = ((this._medAmount == 1) ? 0 : 1);
			IL_00C3:
			if (amount < this._medAmount)
			{
				if (amount > this._lowAmount)
				{
					this._medAmount = amount;
				}
				else
				{
					this._lowAmount = amount;
					this._medAmount = 0;
				}
				this._highAmount = 0;
			}
			else if (amount < this._highAmount)
			{
				this._highAmount = ((amount == this._medAmount) ? 0 : amount);
			}
			this.PrepButton(this._lowText, this._lowAmount);
			this.PrepButton(this._medText, this._medAmount);
			this.PrepButton(this._highText, this._highAmount);
		}

		public bool IsHovering()
		{
			Vector2 vector;
			return RectTransformUtility.ScreenPointToLocalPointInRectangle(this._myTransform, Input.mousePosition, null, out vector) && vector.x > this._minX && vector.x < this._maxX && vector.y > this._minY && vector.y < this._maxY;
		}

		private void PrepButton(TextMeshProUGUI t, int amount)
		{
			t.transform.parent.gameObject.SetActive(amount > 0);
			if (amount <= 0)
			{
				return;
			}
			t.text = amount.ToString() + "x";
		}

		private void Update()
		{
			ReferenceHub referenceHub;
			this._allowDropping = ReferenceHub.TryGetLocalHub(out referenceHub) && IAmmoDropPreventer.CanDropAmmo(referenceHub, this._targetItem.ItemTypeId);
			this._dropCanvas.alpha = (this._allowDropping ? 1f : 0.1f);
		}

		[SerializeField]
		private RawImage _iconImg;

		[SerializeField]
		private TextMeshProUGUI _nameTxt;

		[SerializeField]
		private RectTransform _myTransform;

		[SerializeField]
		private float _minX;

		[SerializeField]
		private float _maxX;

		[SerializeField]
		private float _minY;

		[SerializeField]
		private float _maxY;

		[SerializeField]
		private Graphic[] _paintableParts;

		[SerializeField]
		private TextMeshProUGUI _amountIndicator;

		[SerializeField]
		private TextMeshProUGUI _lowText;

		[SerializeField]
		private TextMeshProUGUI _medText;

		[SerializeField]
		private TextMeshProUGUI _highText;

		[SerializeField]
		private CanvasGroup _dropCanvas;

		private const float DropInvalidAlpha = 0.1f;

		private int _lowAmount;

		private int _medAmount;

		private int _highAmount;

		private bool _allowDropping;

		private ItemBase _targetItem;
	}
}

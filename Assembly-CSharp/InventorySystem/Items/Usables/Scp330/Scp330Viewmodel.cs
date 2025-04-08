using System;
using CursorManagement;
using CustomPlayerEffects;
using InventorySystem.GUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Usables.Scp330
{
	public class Scp330Viewmodel : UsableItemViewmodel, ICursorOverride
	{
		public CursorOverrideMode CursorOverride { get; private set; }

		public bool LockMovement
		{
			get
			{
				return false;
			}
		}

		public override void InitLocal(ItemBase parent)
		{
			base.InitLocal(parent);
			this._bag = parent as Scp330Bag;
		}

		public override void InitSpectator(ReferenceHub ply, ItemIdentifier id, bool wasEquipped)
		{
			Scp330NetworkHandler.OnClientSelectMessageReceived += this.HandleSelectMessage;
			base.InitSpectator(ply, id, wasEquipped);
			this.OnEquipped();
		}

		internal override void OnEquipped()
		{
			this._openDelay = true;
			this._cancelled = false;
			this._descriptionGroup.alpha = 0f;
			this._displayedCandy = CandyKindID.None;
			if (base.IsLocal)
			{
				CursorManager.Register(this);
				this.CursorOverride = CursorOverrideMode.NoOverride;
			}
			else
			{
				this._selectorGroup.gameObject.SetActive(false);
			}
			base.OnEquipped();
		}

		protected void Update()
		{
			if (!base.IsLocal)
			{
				return;
			}
			KeyCode key = NewInput.GetKey(ActionName.Inventory, KeyCode.None);
			bool flag = !InventoryGuiController.ToggleInventory.Value;
			if (!Input.GetKeyDown(KeyCode.Escape) && (flag || !Input.GetKeyDown(key)) && (!flag || Input.GetKey(key)))
			{
				return;
			}
			this.CancelSelector(false);
		}

		protected override void LateUpdate()
		{
			base.LateUpdate();
			if (!base.IsLocal)
			{
				return;
			}
			CandyKindID candyKindID = (this._bag.IsCandySelected ? this._bag.Candies[this._bag.SelectedCandyId] : CandyKindID.None);
			this.SetCandyModel(candyKindID);
			this.DisplaySelector(candyKindID);
			if (candyKindID != CandyKindID.None)
			{
				return;
			}
			ushort num = 0;
			while ((int)num < this._selector.OrganizedContent.Length)
			{
				this._selector.OrganizedContent[(int)num] = (((int)num < this._bag.Candies.Count) ? (num + 1) : 0);
				num += 1;
			}
			if (this._openDelay)
			{
				this._openDelay = false;
				return;
			}
			ushort num2;
			InventoryGuiAction inventoryGuiAction = this._selector.DisplayAndSelectItems(null, out num2);
			if (inventoryGuiAction != InventoryGuiAction.Drop)
			{
				if (inventoryGuiAction == InventoryGuiAction.Select)
				{
					AmnesiaItems amnesiaItems;
					if (base.Hub.playerEffectsController.TryGetEffect<AmnesiaItems>(out amnesiaItems) && amnesiaItems.IsEnabled)
					{
						amnesiaItems.ExecutePulse();
						this.CancelSelector(false);
					}
					else
					{
						if (num2 == 0 || base.ParentItem.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
						{
							this.CancelSelector(false);
						}
						Scp330Viewmodel.CandyObject candyObject;
						if (this.TryGetCandyObject(this._bag.Candies[Mathf.Clamp((int)(num2 - 1), 0, this._bag.Candies.Count)], out candyObject))
						{
							this._bag.UsingSfxClip = candyObject.EatingSound;
						}
						this._bag.SelectCandy((int)(num2 - 1));
					}
				}
			}
			else
			{
				if (num2 == 0)
				{
					return;
				}
				this._bag.DropCandy((int)(num2 - 1));
			}
			this.DisplayDescriptions((num2 == 0 || this._bag.Candies.Count == 0 || (int)num2 > this._bag.Candies.Count) ? CandyKindID.None : this._bag.Candies[(int)(num2 - 1)]);
		}

		private void HandleSelectMessage(SelectScp330Message msg)
		{
			if (msg.Serial != base.ItemId.SerialNumber)
			{
				return;
			}
			this.SetCandyModel((CandyKindID)msg.CandyID);
			this.OnUsingStarted();
		}

		private void OnDisable()
		{
			if (!base.IsLocal)
			{
				Scp330NetworkHandler.OnClientSelectMessageReceived -= this.HandleSelectMessage;
				return;
			}
			CursorManager.Unregister(this);
		}

		private void SetCandyModel(CandyKindID id)
		{
			foreach (Scp330Viewmodel.CandyObject candyObject in this._candies)
			{
				candyObject.HandObject.SetActive(candyObject.KindID == id);
			}
		}

		private void DisplaySelector(CandyKindID id)
		{
			bool flag = id == CandyKindID.None && !this._cancelled;
			this._selectorGroup.alpha = Mathf.Clamp01(this._selectorGroup.alpha + (float)(flag ? 10 : (-10)) * Time.deltaTime);
			this._selectorGroup.gameObject.SetActive(this._selectorGroup.alpha > 0f);
			this.CursorOverride = (flag ? CursorOverrideMode.Free : CursorOverrideMode.Centered);
			for (int i = 0; i < this._selectorSlots.Length; i++)
			{
				Scp330Viewmodel.CandyObject candyObject;
				if (i < this._bag.Candies.Count && this.TryGetCandyObject(this._bag.Candies[i], out candyObject))
				{
					this._selectorSlots[i].texture = candyObject.Icon;
					this._selectorSlots[i].enabled = true;
				}
				else
				{
					this._selectorSlots[i].enabled = false;
				}
			}
		}

		private void DisplayDescriptions(CandyKindID candy)
		{
			bool flag = candy == CandyKindID.None;
			if (this._displayedCandy == candy && !flag)
			{
				this._descriptionGroup.alpha = Mathf.Clamp01(this._descriptionGroup.alpha + 10f * Time.deltaTime);
				return;
			}
			if (this._descriptionGroup.alpha > 0f || flag)
			{
				this._descriptionGroup.alpha = Mathf.Clamp01(this._descriptionGroup.alpha - 10f * Time.deltaTime);
				return;
			}
			string text;
			string text2;
			string text3;
			Scp330Translations.GetCandyTranslation(candy, out text, out text2, out text3);
			this._title.text = text;
			this._description.text = text2;
			this._effects.text = text3;
			this._displayedCandy = candy;
		}

		private bool TryGetCandyObject(CandyKindID id, out Scp330Viewmodel.CandyObject val)
		{
			foreach (Scp330Viewmodel.CandyObject candyObject in this._candies)
			{
				if (candyObject.KindID == id)
				{
					val = candyObject;
					return true;
				}
			}
			val = default(Scp330Viewmodel.CandyObject);
			return false;
		}

		private void CancelSelector(bool bringBackInventory = false)
		{
			this._cancelled = true;
			this._bag.OwnerInventory.ClientSelectItem(0);
			InventoryGuiController.InventoryVisible = InventoryGuiController.InventoryVisible || bringBackInventory;
		}

		public static AudioClip GetClipForCandy(CandyKindID kind)
		{
			Scp330Bag scp330Bag;
			if (!InventoryItemLoader.TryGetItem<Scp330Bag>(ItemType.SCP330, out scp330Bag))
			{
				return null;
			}
			foreach (Scp330Viewmodel.CandyObject candyObject in (scp330Bag.ViewModel as Scp330Viewmodel)._candies)
			{
				if (candyObject.KindID == kind)
				{
					return candyObject.EatingSound;
				}
			}
			return null;
		}

		[SerializeField]
		private RadialInventory _selector;

		[SerializeField]
		private RawImage[] _selectorSlots;

		[SerializeField]
		private CanvasGroup _selectorGroup;

		[SerializeField]
		private CanvasGroup _descriptionGroup;

		[SerializeField]
		private Scp330Viewmodel.CandyObject[] _candies;

		[SerializeField]
		private TextMeshProUGUI _title;

		[SerializeField]
		private TextMeshProUGUI _description;

		[SerializeField]
		private TextMeshProUGUI _effects;

		private Scp330Bag _bag;

		private bool _openDelay;

		private bool _cancelled;

		private CandyKindID _displayedCandy;

		[Serializable]
		private struct CandyObject
		{
			public CandyKindID KindID;

			public GameObject HandObject;

			public Texture Icon;

			public AudioClip EatingSound;
		}
	}
}

using System;
using Interactables;
using Interactables.Verification;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class AttachmentPresetSelector : MonoBehaviour, IClientInteractable, IInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		private void Start()
		{
			for (int i = 0; i < this._currentPresetIndicators.Length; i++)
			{
				this._currentPresetIndicators[i].text = ((i == 0) ? Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.Custom) : string.Format(Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.PresetId), i));
			}
			string text = "[ " + Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.SaveAttachments) + " ]";
			TextMeshProUGUI[] saveButtons = this._saveButtons;
			for (int j = 0; j < saveButtons.Length; j++)
			{
				saveButtons[j].text = text;
			}
		}

		public void ProcessButton(int id)
		{
			if (id == 253)
			{
				this._selectorRef.ToggleSummaryScreen();
				return;
			}
			if (id == 254)
			{
				this._selectorRef.ResetAttachments();
				return;
			}
			if (id > 100)
			{
				this._selectorRef.SaveAsPreset(id - 100);
				return;
			}
			this._selectorRef.LoadPreset(id);
		}

		private void LateUpdate()
		{
			if (this._selectorRef.SelectedFirearm == null)
			{
				this._rootObject.SetActive(false);
				return;
			}
			int preset = AttachmentPreferences.GetPreset(this._selectorRef.SelectedFirearm.ItemTypeId);
			for (int i = 0; i < Mathf.Min(this._currentPresetIndicators.Length, this._saveButtons.Length); i++)
			{
				this._saveButtons[i].gameObject.SetActive(this._selectorRef.CanSaveAsPreference(i));
				this._currentPresetIndicators[i].color = ((preset == i) ? this._currentColor : this._normalColor);
			}
			this._rootObject.SetActive(true);
		}

		public void ClientInteract(InteractableCollider collider)
		{
			this.ProcessButton((int)collider.ColliderId);
		}

		[SerializeField]
		private AttachmentSelectorBase _selectorRef;

		[SerializeField]
		private GameObject _rootObject;

		[SerializeField]
		private TextMeshProUGUI[] _saveButtons;

		[SerializeField]
		private TextMeshProUGUI[] _currentPresetIndicators;

		[SerializeField]
		private Color _normalColor;

		[SerializeField]
		private Color _currentColor;

		private const byte SaveOffset = 100;

		private const byte ResetAttachmentsCode = 254;

		private const byte SummaryToggleCode = 253;
	}
}

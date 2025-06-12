using Interactables;
using Interactables.Verification;
using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments;

public class AttachmentPresetSelector : MonoBehaviour, IClientInteractable, IInteractable
{
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

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	private void Start()
	{
		for (int i = 0; i < this._currentPresetIndicators.Length; i++)
		{
			this._currentPresetIndicators[i].text = ((i == 0) ? Translations.Get(AttachmentEditorsTranslation.Custom) : string.Format(Translations.Get(AttachmentEditorsTranslation.PresetId), i));
		}
		string text = "[ " + Translations.Get(AttachmentEditorsTranslation.SaveAttachments) + " ]";
		TextMeshProUGUI[] saveButtons = this._saveButtons;
		for (int j = 0; j < saveButtons.Length; j++)
		{
			saveButtons[j].text = text;
		}
	}

	public void ProcessButton(int id)
	{
		if (id != 253)
		{
			if (id == 254)
			{
				this._selectorRef.ResetAttachments();
			}
			else if (id > 100)
			{
				this._selectorRef.SaveAsPreset(id - 100);
			}
			else
			{
				this._selectorRef.LoadPreset(id);
			}
		}
		else
		{
			this._selectorRef.ToggleSummaryScreen();
		}
	}

	private void LateUpdate()
	{
		if (this._selectorRef.SelectedFirearm == null)
		{
			this._rootObject.SetActive(value: false);
			return;
		}
		int preset = AttachmentPreferences.GetPreset(this._selectorRef.SelectedFirearm.ItemTypeId);
		for (int i = 0; i < Mathf.Min(this._currentPresetIndicators.Length, this._saveButtons.Length); i++)
		{
			this._saveButtons[i].gameObject.SetActive(this._selectorRef.CanSaveAsPreference(i));
			this._currentPresetIndicators[i].color = ((preset == i) ? this._currentColor : this._normalColor);
		}
		this._rootObject.SetActive(value: true);
	}

	public void ClientInteract(InteractableCollider collider)
	{
		this.ProcessButton(collider.ColliderId);
	}
}

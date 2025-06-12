using InventorySystem.Items.Firearms.Attachments;
using TMPro;
using ToggleableMenus;
using UnityEngine;

namespace PlayerRoles.Spectating;

public class SpectatorAttachmentsWindowToggler : SimpleToggleableMenu
{
	[SerializeField]
	private TextMeshProUGUI _toggleHint;

	public override bool CanToggle => base.gameObject.activeInHierarchy;

	protected override void Awake()
	{
		base.Awake();
		this._toggleHint.text = string.Format(Translations.Get(AttachmentEditorsTranslation.SpectatorEditorTip), new ReadableKeyCode(base.MenuActionKey));
	}

	private void OnDisable()
	{
	}
}

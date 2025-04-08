using System;
using InventorySystem.Items.Firearms.Attachments;
using TMPro;
using ToggleableMenus;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public class SpectatorAttachmentsWindowToggler : SimpleToggleableMenu
	{
		public override bool CanToggle
		{
			get
			{
				return base.gameObject.activeInHierarchy;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this._toggleHint.text = string.Format(Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.SpectatorEditorTip), new ReadableKeyCode(this.MenuActionKey));
		}

		private void OnDisable()
		{
			this.IsEnabled = false;
		}

		[SerializeField]
		private TextMeshProUGUI _toggleHint;
	}
}

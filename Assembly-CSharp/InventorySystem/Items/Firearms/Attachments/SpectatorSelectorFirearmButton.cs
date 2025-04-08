using System;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class SpectatorSelectorFirearmButton : Button
	{
		public void Setup(SpectatorAttachmentSelector selector, Firearm fa)
		{
			this._img = base.GetComponent<RawImage>();
			this._selector = selector;
			this._img.texture = fa.Icon;
			this._normalColor = Color.Lerp(Color.clear, Color.white, 0.75f);
			this._fa = fa;
		}

		private void Update()
		{
			this._img.color = Color.Lerp(this._img.color, (this._selector.SelectedFirearm == this._fa) ? Color.white : this._normalColor, Time.deltaTime * 10f);
		}

		public void Click()
		{
			this._selector.SelectFirearm(this._fa);
		}

		private SpectatorAttachmentSelector _selector;

		private Firearm _fa;

		private RawImage _img;

		private Color _normalColor;

		private const float NormalColor = 0.75f;
	}
}

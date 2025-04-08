using System;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI
{
	public class Scp079NotificationEntry : Scp079GuiElementBase
	{
		public IScp079Notification Content { get; internal set; }

		public TextMeshProUGUI Text
		{
			get
			{
				return this._text;
			}
		}

		[SerializeField]
		private TextMeshProUGUI _text;
	}
}

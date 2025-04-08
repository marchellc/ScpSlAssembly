using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryRecordingRearranger : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
	{
		public void OnPointerDown(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
			{
				return;
			}
			this._menu.BeginDrag(this._icon);
		}

		[SerializeField]
		private MimicryRecordingsMenu _menu;

		[SerializeField]
		private MimicryRecordingIcon _icon;
	}
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerRoles.Spectating
{
	public class OverwatchHelpButton : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public void OnPointerEnter(PointerEventData eventData)
		{
			this.HelpDialog.SetActive(true);
			this.HelpText.text = this.HelpTextReplacer.DisplayText;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			this.HelpDialog.SetActive(false);
		}

		public GameObject HelpDialog;

		public TextMeshProUGUI HelpText;

		public TextLanguageReplacer HelpTextReplacer;
	}
}

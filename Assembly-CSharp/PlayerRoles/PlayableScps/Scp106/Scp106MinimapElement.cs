using System;
using MapGeneration;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp106
{
	public class Scp106MinimapElement : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
	{
		public Image Img { get; private set; }

		public RectTransform Rt { get; private set; }

		public RoomIdentifier Room { get; internal set; }

		public static bool AnyHighlighted { get; private set; }

		public static Scp106MinimapElement LastHighlighted { get; private set; }

		public void OnPointerEnter(PointerEventData eventData)
		{
			Scp106MinimapElement.LastHighlighted = this;
			Scp106MinimapElement.AnyHighlighted = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (Scp106MinimapElement.LastHighlighted != this)
			{
				return;
			}
			Scp106MinimapElement.LastHighlighted = null;
			Scp106MinimapElement.AnyHighlighted = false;
		}
	}
}

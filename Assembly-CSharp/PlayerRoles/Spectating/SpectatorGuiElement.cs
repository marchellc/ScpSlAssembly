using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerRoles.Spectating;

public class SpectatorGuiElement : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private static SpectatorGuiElement _lastHighlight;

	public static bool AnyHighlighted { get; private set; }

	public void OnPointerEnter(PointerEventData eventData)
	{
		SpectatorGuiElement._lastHighlight = this;
		SpectatorGuiElement.AnyHighlighted = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (!(SpectatorGuiElement._lastHighlight != this))
		{
			SpectatorGuiElement._lastHighlight = null;
			SpectatorGuiElement.AnyHighlighted = false;
		}
	}

	private void OnDisable()
	{
		this.OnPointerExit(null);
	}

	private void OnDestroy()
	{
		this.OnPointerExit(null);
	}
}

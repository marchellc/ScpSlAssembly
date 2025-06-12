using UnityEngine;
using UnityEngine.EventSystems;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryTooltipTarget : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler, IPointerDownHandler
{
	[SerializeField]
	private Scp939HudTranslation _targetHint;

	private static MimicryTooltipTarget _curTarget;

	public void OnPointerEnter(PointerEventData eventData)
	{
		MimicryTooltipTarget._curTarget = this;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.Deselect();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		this.Deselect();
	}

	private void OnDisable()
	{
		this.Deselect();
	}

	private void Deselect()
	{
		if (!(MimicryTooltipTarget._curTarget != this))
		{
			MimicryTooltipTarget._curTarget = null;
		}
	}

	internal static bool TryGetHint(out Scp939HudTranslation hint)
	{
		if (MimicryTooltipTarget._curTarget == null)
		{
			hint = Scp939HudTranslation.PressKeyToLunge;
			return false;
		}
		hint = MimicryTooltipTarget._curTarget._targetHint;
		return true;
	}
}

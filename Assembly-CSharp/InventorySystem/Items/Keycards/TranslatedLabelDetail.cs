using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class TranslatedLabelDetail : DetailBase
{
	public enum KeycardLabelTranslation
	{
		Scientist,
		Janitor,
		ResearchSupervisor,
		ContEngineer,
		SecurityGuard,
		ZoneManager,
		FacilityManager,
		SurfaceAccessPassNormal,
		SurfaceAccessPassUsed
	}

	[SerializeField]
	private KeycardLabelTranslation _translation;

	[SerializeField]
	private Color _textColor;

	public override void ApplyDetail(KeycardGfx gfxTarget, KeycardItem template)
	{
		string text = Translations.Get(this._translation);
		TMP_Text[] keycardLabels = gfxTarget.KeycardLabels;
		foreach (TMP_Text obj in keycardLabels)
		{
			obj.text = text;
			obj.color = this._textColor;
		}
	}
}

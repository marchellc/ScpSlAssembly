using System.Text;
using PlayerRoles.Subroutines;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public class Scp079AuxGui : Scp079BarBaseGui
{
	[SerializeField]
	private TextMeshProUGUI _textBlockers;

	[SerializeField]
	private float _blockerHeaderSize;

	private Scp079AuxManager _auxManager;

	private StringBuilder _sb;

	private string _reducedHeader;

	private string _suspendedHeader;

	private const string Format = "{0} / {1}";

	protected override string Text => $"{this._auxManager.CurrentAuxFloored} / {this._auxManager.MaxAux}";

	protected override float FillAmount => this._auxManager.CurrentAux / this._auxManager.MaxAux;

	internal override void Init(Scp079Role role, ReferenceHub owner)
	{
		base.Init(role, owner);
		role.SubroutineModule.TryGetSubroutine<Scp079AuxManager>(out this._auxManager);
		this._sb = new StringBuilder(255);
		this._reducedHeader = this.BuildHeader(Scp079HudTranslation.AuxRegenReduced);
		this._suspendedHeader = this.BuildHeader(Scp079HudTranslation.AuxRegenSuspended);
	}

	private string BuildHeader(Scp079HudTranslation header)
	{
		this._sb.Clear();
		this._sb.Append("<size=");
		this._sb.Append(this._blockerHeaderSize);
		this._sb.Append(">");
		this._sb.Append(Translations.Get(header));
		this._sb.Append("</size>");
		string text = this._sb.ToString();
		RectTransform rectTransform = this._textBlockers.rectTransform;
		rectTransform.sizeDelta = Vector2.Max(rectTransform.sizeDelta, this._textBlockers.GetPreferredValues(text));
		return text;
	}

	protected override void Update()
	{
		base.Update();
		this._sb.Clear();
		bool flag = false;
		float num = 1f;
		SubroutineBase[] allSubroutines = base.Role.SubroutineModule.AllSubroutines;
		for (int i = 0; i < allSubroutines.Length; i++)
		{
			if (allSubroutines[i] is IScp079AuxRegenModifier { AuxRegenMultiplier: var auxRegenMultiplier } scp079AuxRegenModifier && !(auxRegenMultiplier >= 1f))
			{
				flag = true;
				num *= auxRegenMultiplier;
				this._sb.Append("\n");
				this._sb.Append(scp079AuxRegenModifier.AuxReductionMessage);
			}
		}
		if (flag)
		{
			if (num <= 0f)
			{
				this._sb.Insert(0, this._suspendedHeader);
			}
			else
			{
				this._sb.Insert(0, string.Format(this._reducedHeader, num.ToString("0.##%")));
			}
		}
		this._textBlockers.text = this._sb.ToString();
	}
}

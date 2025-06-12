using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.UI;

public class RagdollCleanupTime : MonoBehaviour
{
	private string DisabledString;

	private string SecondsString;

	private string SecondString;

	public InputField Text;

	public Text SecondsText;

	public Slider Slider;

	public void Start()
	{
		this.DisabledString = TranslationReader.Get("NewMainMenu", 73, "Disabled");
		this.SecondsString = TranslationReader.Get("NewMainMenu", 74, "Seconds");
		this.SecondString = TranslationReader.Get("NewMainMenu", 86, "Second");
		int num = PlayerPrefsSl.Get("ragdoll_cleanup", 0);
		if (num <= 0)
		{
			this.Text.SetTextWithoutNotify(string.Empty);
			this.SecondsText.text = this.DisabledString;
		}
		else
		{
			this.SecondsText.text = ((num == 1) ? this.SecondString : this.SecondsString);
			this.Text.SetTextWithoutNotify(num.ToString());
			Timing.RunCoroutine(this.SetPositionOneFrameLater());
		}
		this.Slider.SetValueWithoutNotify(num);
	}

	public void OnValueChanged(float value)
	{
		Mathf.Clamp(value, 0f, 604800f);
		if (value <= 0f)
		{
			this.Text.SetTextWithoutNotify(string.Empty);
			this.SecondsText.text = this.DisabledString;
		}
		else
		{
			this.SecondsText.text = ((value == 1f) ? this.SecondString : this.SecondsString);
			this.Text.SetTextWithoutNotify(value.ToString());
		}
		Timing.RunCoroutine(this.SetPositionOneFrameLater());
		this.Slider.SetValueWithoutNotify(value);
		PlayerPrefsSl.Set("ragdoll_cleanup", (int)value);
	}

	private IEnumerator<float> SetPositionOneFrameLater()
	{
		yield return float.NegativeInfinity;
		this.SecondsText.rectTransform.anchoredPosition = new Vector2(this.Text.textComponent.preferredWidth + 10f, 0f);
	}

	public void OnStringValueChanged(string value)
	{
		if (value.Length == 0)
		{
			this.OnValueChanged(0f);
		}
		if (float.TryParse(value, out var result))
		{
			this.OnValueChanged(result);
		}
	}
}

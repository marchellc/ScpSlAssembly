using UnityEngine;
using UnityEngine.UI;

public class StatusBar : MonoBehaviour
{
	public enum AutoHideType
	{
		WhenFull,
		WhenEmpty,
		AlwaysVisible
	}

	[Tooltip("Above which object should this be displayed?")]
	public StatusBar MasterBar;

	[Tooltip("Slider which the script will affect")]
	public Slider TargetSlider;

	[Tooltip("All graphics that should be faded in/out")]
	public CanvasRenderer[] TargetGraphics;

	[Tooltip("Y-axis offset of the MasterTransform.position")]
	public float FixedDistance;

	public AutoHideType AutohideOption;

	public bool HiddenByDefault;

	public float FadeInSpeed;

	public float FadeOutSpeed;

	public float MoveSpeed;

	private float CurAlpha
	{
		get
		{
			if (this.TargetGraphics.Length != 0)
			{
				return this.TargetGraphics[0].GetAlpha();
			}
			return 1f;
		}
	}

	private Vector3 LocalPosition
	{
		get
		{
			return (base.transform as RectTransform).localPosition;
		}
		set
		{
			(base.transform as RectTransform).localPosition = value;
		}
	}

	private void OnEnable()
	{
		if (this.HiddenByDefault)
		{
			this.TargetGraphics.ForEach(delegate(CanvasRenderer x)
			{
				x.SetAlpha(0f);
			});
		}
	}

	private void Move(float targetY, bool bypassAnimation)
	{
		Vector3 localPosition = this.LocalPosition;
		if (bypassAnimation)
		{
			localPosition.y = targetY;
		}
		else
		{
			float num = Time.deltaTime * this.MoveSpeed;
			float num2 = targetY - localPosition.y;
			if (num > Mathf.Abs(num2))
			{
				localPosition.y = targetY;
			}
			else
			{
				localPosition.y += num * (float)((num2 > 0f) ? 1 : (-1));
			}
		}
		this.LocalPosition = localPosition;
	}

	private void Update()
	{
		this.UpdateBar(bypassAnims: false);
	}

	public void UpdateBar(bool bypassAnims)
	{
		if (this.MasterBar != null)
		{
			StatusBar masterBar = this.MasterBar;
			while (masterBar.TargetGraphics.Length != 0 && masterBar.CurAlpha <= 0.1f)
			{
				if (masterBar.MasterBar == null)
				{
					return;
				}
				masterBar = masterBar.MasterBar;
			}
			this.Move(masterBar.LocalPosition.y + this.FixedDistance, bypassAnims || this.CurAlpha <= 0f);
		}
		if (this.AutohideOption != AutoHideType.AlwaysVisible)
		{
			float num = this.FadeInSpeed;
			if ((this.AutohideOption == AutoHideType.WhenEmpty && this.TargetSlider.value == this.TargetSlider.minValue) || (this.AutohideOption == AutoHideType.WhenFull && this.TargetSlider.value == this.TargetSlider.maxValue))
			{
				num = 0f - this.FadeOutSpeed;
			}
			if (bypassAnims)
			{
				this.SetAlpha(Mathf.Sign(num));
			}
			else
			{
				this.SetAlpha(this.TargetGraphics[0].GetAlpha() + num * Time.deltaTime);
			}
		}
	}

	public void SetAlpha(float a)
	{
		CanvasRenderer[] targetGraphics = this.TargetGraphics;
		for (int i = 0; i < targetGraphics.Length; i++)
		{
			targetGraphics[i].SetAlpha(Mathf.Clamp01(a));
		}
	}
}

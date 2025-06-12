using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments.Components;

public class ReflexSightConfigWindow : AttachmentConfigWindow
{
	[SerializeField]
	private RectTransform _shapeTemplate;

	[SerializeField]
	private RectTransform _colorTemplate;

	[SerializeField]
	private RectTransform _brightnessTemplate;

	[SerializeField]
	private RectTransform _buttonReduce;

	[SerializeField]
	private RectTransform _buttonEnlarge;

	[SerializeField]
	private RectTransform[] _ignoreRaycastRects;

	[SerializeField]
	private TextMeshProUGUI _textPercent;

	[SerializeField]
	private Color _selectedColor;

	[SerializeField]
	private Color _normalColor;

	private Image[] _shapeInstances;

	private Image[] _colorInstances;

	private Image[] _brightnessInstances;

	private static Color32[] _colorsByHue;

	public override void Setup(AttachmentSelectorBase selector, Attachment attachment, RectTransform toFit)
	{
		base.Setup(selector, attachment, toFit);
		this._ignoreRaycastRects.ForEach(delegate(RectTransform x)
		{
			selector.RegisterAction(x, null);
		});
		if (ReflexSightConfigWindow._colorsByHue == null)
		{
			ReflexSightConfigWindow._colorsByHue = ReflexSightAttachment.Colors.OrderBy(GetHue).ToArray();
		}
		ReflexSightAttachment reflex = attachment as ReflexSightAttachment;
		if ((object)reflex != null)
		{
			ReflexSightAttachment reflexSightAttachment = reflex;
			reflexSightAttachment.OnValuesChanged = (Action)Delegate.Combine(reflexSightAttachment.OnValuesChanged, new Action(UpdateValues));
			this._shapeInstances = this.GenerateOptions(reflex.TextureOptions.Reticles, this._shapeTemplate, delegate(int x)
			{
				reflex.ModifyValues(x);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().texture = reflex.TextureOptions[z];
			});
			this._colorInstances = this.GenerateOptions(ReflexSightConfigWindow._colorsByHue, this._colorTemplate, delegate(int x)
			{
				ReflexSightAttachment reflexSightAttachment2 = reflex;
				int? color = this.TranslateIndex(ReflexSightConfigWindow._colorsByHue, ReflexSightAttachment.Colors, x);
				int? brightness = 0;
				reflexSightAttachment2.ModifyValues(null, color, null, brightness);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().color = ReflexSightConfigWindow._colorsByHue[z] * this._selectedColor;
			});
			this._brightnessInstances = this.GenerateOptions(ReflexSightAttachment.BrightnessLevels, this._brightnessTemplate, delegate(int x)
			{
				ReflexSightAttachment reflexSightAttachment2 = reflex;
				int? brightness = x;
				reflexSightAttachment2.ModifyValues(null, null, null, brightness);
			}, null);
			selector.RegisterAction(this._buttonReduce, delegate
			{
				ReflexSightAttachment reflexSightAttachment2 = reflex;
				int? size = reflex.CurSizeIndex - 1;
				reflexSightAttachment2.ModifyValues(null, null, size);
			});
			selector.RegisterAction(this._buttonEnlarge, delegate
			{
				ReflexSightAttachment reflexSightAttachment2 = reflex;
				int? size = reflex.CurSizeIndex + 1;
				reflexSightAttachment2.ModifyValues(null, null, size);
			});
			this.UpdateValues();
		}
		static float GetHue(Color32 color)
		{
			Color.RGBToHSV(color, out var H, out var _, out var _);
			return H;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (base.Attachment is ReflexSightAttachment reflexSightAttachment && !(reflexSightAttachment == null))
		{
			reflexSightAttachment.OnValuesChanged = (Action)Delegate.Remove(reflexSightAttachment.OnValuesChanged, new Action(UpdateValues));
		}
	}

	private void UpdateValues()
	{
		ReflexSightAttachment reflexSightAttachment = base.Attachment as ReflexSightAttachment;
		for (int i = 0; i < this._shapeInstances.Length; i++)
		{
			this._shapeInstances[i].color = ((i == reflexSightAttachment.CurTextureIndex) ? this._selectedColor : this._normalColor);
		}
		int num = ((reflexSightAttachment.CurBrightnessIndex > 0) ? (-1) : this.TranslateIndex(ReflexSightAttachment.Colors, ReflexSightConfigWindow._colorsByHue, reflexSightAttachment.CurColorIndex));
		for (int j = 0; j < this._colorInstances.Length; j++)
		{
			this._colorInstances[j].color = ((j == num) ? this._selectedColor : this._normalColor);
		}
		this._textPercent.text = Mathf.RoundToInt(ReflexSightAttachment.Sizes[reflexSightAttachment.CurSizeIndex] * 100f) + "%";
		for (int k = 0; k < this._brightnessInstances.Length; k++)
		{
			Image obj = this._brightnessInstances[k];
			obj.color = ((k == reflexSightAttachment.CurBrightnessIndex) ? this._selectedColor : this._normalColor);
			Color32 color = Color32.Lerp(ReflexSightAttachment.Colors[reflexSightAttachment.CurColorIndex], t: ReflexSightAttachment.BrightnessLevels[k], b: Color.white);
			obj.GetComponentInChildren<RawImage>().color = color * this._selectedColor;
		}
	}

	private Image[] GenerateOptions<T>(T[] array, RectTransform template, Action<int> onClick, Action<RectTransform, int> setupMethod)
	{
		int num = array.Length;
		if (num <= 1)
		{
			return new Image[0];
		}
		Image[] array2 = new Image[num];
		for (int i = 0; i < num; i++)
		{
			int copyI = i;
			RectTransform rectTransform = UnityEngine.Object.Instantiate(template, template.parent);
			base.Selector.RegisterAction(rectTransform, delegate
			{
				onClick?.Invoke(copyI);
			});
			setupMethod?.Invoke(rectTransform, copyI);
			array2[i] = rectTransform.GetComponent<Image>();
			rectTransform.gameObject.SetActive(value: true);
		}
		return array2;
	}

	private int TranslateIndex(Color32[] fromArray, Color32[] toArray, int index, int fallback = -1)
	{
		Color32 color = fromArray[index];
		for (int i = 0; i < toArray.Length; i++)
		{
			Color32 color2 = toArray[i];
			if (color2.r == color.r && color2.g == color.g && color2.b == color.b)
			{
				return i;
			}
		}
		return fallback;
	}
}

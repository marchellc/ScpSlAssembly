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
		_ignoreRaycastRects.ForEach(delegate(RectTransform x)
		{
			selector.RegisterAction(x, null);
		});
		if (_colorsByHue == null)
		{
			_colorsByHue = ReflexSightAttachment.Colors.OrderBy(GetHue).ToArray();
		}
		ReflexSightAttachment reflex = attachment as ReflexSightAttachment;
		if ((object)reflex != null)
		{
			ReflexSightAttachment reflexSightAttachment = reflex;
			reflexSightAttachment.OnValuesChanged = (Action)Delegate.Combine(reflexSightAttachment.OnValuesChanged, new Action(UpdateValues));
			_shapeInstances = GenerateOptions(reflex.TextureOptions.Reticles, _shapeTemplate, delegate(int x)
			{
				reflex.ModifyValues(x, null, null, null);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().texture = reflex.TextureOptions[z];
			});
			_colorInstances = GenerateOptions(_colorsByHue, _colorTemplate, delegate(int x)
			{
				ReflexSightAttachment reflexSightAttachment2 = reflex;
				int? color2 = TranslateIndex(_colorsByHue, ReflexSightAttachment.Colors, x);
				int? brightness = 0;
				reflexSightAttachment2.ModifyValues(null, color2, null, brightness);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().color = _colorsByHue[z] * _selectedColor;
			});
			_brightnessInstances = GenerateOptions(ReflexSightAttachment.BrightnessLevels, _brightnessTemplate, delegate(int x)
			{
				ReflexSightAttachment reflexSightAttachment3 = reflex;
				int? brightness2 = x;
				reflexSightAttachment3.ModifyValues(null, null, null, brightness2);
			}, null);
			selector.RegisterAction(_buttonReduce, delegate
			{
				ReflexSightAttachment reflexSightAttachment4 = reflex;
				int? size = reflex.CurSizeIndex - 1;
				reflexSightAttachment4.ModifyValues(null, null, size, null);
			});
			selector.RegisterAction(_buttonEnlarge, delegate
			{
				ReflexSightAttachment reflexSightAttachment5 = reflex;
				int? size2 = reflex.CurSizeIndex + 1;
				reflexSightAttachment5.ModifyValues(null, null, size2, null);
			});
			UpdateValues();
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
		for (int i = 0; i < _shapeInstances.Length; i++)
		{
			_shapeInstances[i].color = ((i == reflexSightAttachment.CurTextureIndex) ? _selectedColor : _normalColor);
		}
		int num = ((reflexSightAttachment.CurBrightnessIndex > 0) ? (-1) : TranslateIndex(ReflexSightAttachment.Colors, _colorsByHue, reflexSightAttachment.CurColorIndex));
		for (int j = 0; j < _colorInstances.Length; j++)
		{
			_colorInstances[j].color = ((j == num) ? _selectedColor : _normalColor);
		}
		_textPercent.text = Mathf.RoundToInt(ReflexSightAttachment.Sizes[reflexSightAttachment.CurSizeIndex] * 100f) + "%";
		for (int k = 0; k < _brightnessInstances.Length; k++)
		{
			Image obj = _brightnessInstances[k];
			obj.color = ((k == reflexSightAttachment.CurBrightnessIndex) ? _selectedColor : _normalColor);
			Color32 color = Color32.Lerp(ReflexSightAttachment.Colors[reflexSightAttachment.CurColorIndex], t: ReflexSightAttachment.BrightnessLevels[k], b: Color.white);
			obj.GetComponentInChildren<RawImage>().color = color * _selectedColor;
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

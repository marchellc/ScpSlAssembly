using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	public class ReflexSightConfigWindow : AttachmentConfigWindow
	{
		public override void Setup(AttachmentSelectorBase selector, Attachment attachment, RectTransform toFit)
		{
			base.Setup(selector, attachment, toFit);
			this._ignoreRaycastRects.ForEach(delegate(RectTransform x)
			{
				selector.RegisterAction(x, null);
			});
			if (ReflexSightConfigWindow._colorsByHue == null)
			{
				ReflexSightConfigWindow._colorsByHue = ReflexSightAttachment.Colors.OrderBy(new Func<Color32, float>(ReflexSightConfigWindow.<Setup>g__GetHue|13_8)).ToArray<Color32>();
			}
			ReflexSightAttachment reflex = attachment as ReflexSightAttachment;
			if (reflex == null)
			{
				return;
			}
			ReflexSightAttachment reflex6 = reflex;
			reflex6.OnValuesChanged = (Action)Delegate.Combine(reflex6.OnValuesChanged, new Action(this.UpdateValues));
			this._shapeInstances = this.GenerateOptions<Texture>(reflex.TextureOptions.Reticles, this._shapeTemplate, delegate(int x)
			{
				reflex.ModifyValues(new int?(x), null, null, null);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().texture = reflex.TextureOptions[z];
			});
			this._colorInstances = this.GenerateOptions<Color32>(ReflexSightConfigWindow._colorsByHue, this._colorTemplate, delegate(int x)
			{
				ReflexSightAttachment reflex2 = reflex;
				int? num = new int?(this.TranslateIndex(ReflexSightConfigWindow._colorsByHue, ReflexSightAttachment.Colors, x, -1));
				int? num2 = new int?(0);
				reflex2.ModifyValues(null, num, null, num2);
			}, delegate(RectTransform y, int z)
			{
				y.GetComponentInChildren<RawImage>().color = ReflexSightConfigWindow._colorsByHue[z] * this._selectedColor;
			});
			this._brightnessInstances = this.GenerateOptions<float>(ReflexSightAttachment.BrightnessLevels, this._brightnessTemplate, delegate(int x)
			{
				ReflexSightAttachment reflex3 = reflex;
				int? num3 = new int?(x);
				reflex3.ModifyValues(null, null, null, num3);
			}, null);
			selector.RegisterAction(this._buttonReduce, delegate(Vector2 _)
			{
				ReflexSightAttachment reflex4 = reflex;
				int? num4 = new int?(reflex.CurSizeIndex - 1);
				reflex4.ModifyValues(null, null, num4, null);
			});
			selector.RegisterAction(this._buttonEnlarge, delegate(Vector2 _)
			{
				ReflexSightAttachment reflex5 = reflex;
				int? num5 = new int?(reflex.CurSizeIndex + 1);
				reflex5.ModifyValues(null, null, num5, null);
			});
			this.UpdateValues();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			ReflexSightAttachment reflexSightAttachment = base.Attachment as ReflexSightAttachment;
			if (reflexSightAttachment == null || reflexSightAttachment == null)
			{
				return;
			}
			ReflexSightAttachment reflexSightAttachment2 = reflexSightAttachment;
			reflexSightAttachment2.OnValuesChanged = (Action)Delegate.Remove(reflexSightAttachment2.OnValuesChanged, new Action(this.UpdateValues));
		}

		private void UpdateValues()
		{
			ReflexSightAttachment reflexSightAttachment = base.Attachment as ReflexSightAttachment;
			for (int i = 0; i < this._shapeInstances.Length; i++)
			{
				this._shapeInstances[i].color = ((i == reflexSightAttachment.CurTextureIndex) ? this._selectedColor : this._normalColor);
			}
			int num = ((reflexSightAttachment.CurBrightnessIndex > 0) ? (-1) : this.TranslateIndex(ReflexSightAttachment.Colors, ReflexSightConfigWindow._colorsByHue, reflexSightAttachment.CurColorIndex, -1));
			for (int j = 0; j < this._colorInstances.Length; j++)
			{
				this._colorInstances[j].color = ((j == num) ? this._selectedColor : this._normalColor);
			}
			this._textPercent.text = Mathf.RoundToInt(ReflexSightAttachment.Sizes[reflexSightAttachment.CurSizeIndex] * 100f).ToString() + "%";
			for (int k = 0; k < this._brightnessInstances.Length; k++)
			{
				Image image = this._brightnessInstances[k];
				image.color = ((k == reflexSightAttachment.CurBrightnessIndex) ? this._selectedColor : this._normalColor);
				Color32 color = ReflexSightAttachment.Colors[reflexSightAttachment.CurColorIndex];
				float num2 = ReflexSightAttachment.BrightnessLevels[k];
				Color32 color2 = Color32.Lerp(color, Color.white, num2);
				image.GetComponentInChildren<RawImage>().color = color2 * this._selectedColor;
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
				RectTransform rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(template, template.parent);
				base.Selector.RegisterAction(rectTransform, delegate(Vector2 _)
				{
					Action<int> onClick2 = onClick;
					if (onClick2 == null)
					{
						return;
					}
					onClick2(copyI);
				});
				if (setupMethod != null)
				{
					setupMethod(rectTransform, copyI);
				}
				array2[i] = rectTransform.GetComponent<Image>();
				rectTransform.gameObject.SetActive(true);
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

		[CompilerGenerated]
		internal static float <Setup>g__GetHue|13_8(Color32 color)
		{
			float num;
			float num2;
			float num3;
			Color.RGBToHSV(color, out num, out num2, out num3);
			return num;
		}

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
	}
}

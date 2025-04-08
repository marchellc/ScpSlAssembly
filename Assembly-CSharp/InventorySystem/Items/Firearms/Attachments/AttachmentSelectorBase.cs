using System;
using System.Collections.Generic;
using CameraShaking;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Attachments.Formatters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments
{
	public abstract class AttachmentSelectorBase : MonoBehaviour
	{
		public Firearm SelectedFirearm { get; protected set; }

		protected abstract bool UseLookatMode { get; set; }

		private static Vector3 SpinRotation
		{
			get
			{
				return Vector3.back * Time.timeSinceLevelLoad * 100f;
			}
		}

		public void ProcessCollider(byte colId)
		{
			if (this.SelectedFirearm == null)
			{
				return;
			}
			this.RefreshState(this.SelectedFirearm, new byte?(colId));
			if (colId >= 32)
			{
				this.SelectedSlot = (AttachmentSlot)(colId - 32);
				if (this.UseLookatMode)
				{
					this.RefreshState(this.SelectedFirearm, new byte?(colId));
					return;
				}
			}
			else
			{
				this.SelectAttachmentId(colId);
			}
		}

		public void ShowStats(int attachmentId)
		{
			this._isCorrectAttachment = attachmentId >= 0 && attachmentId < this.SelectedFirearm.Attachments.Length;
			if (!this._isCorrectAttachment)
			{
				return;
			}
			Attachment attachment = this.SelectedFirearm.Attachments[attachmentId];
			string text;
			string text2;
			attachment.GetNameAndDescription(out text, out text2);
			this._pros.text = string.Empty;
			this._cons.text = string.Empty;
			this._attachmentName.text = text;
			this._attachmentDescription.text = text2;
			for (int i = 0; i < AttachmentsUtils.TotalNumberOfParams; i++)
			{
				float num;
				AttachmentParamState attachmentParamState;
				attachment.GetParameterData(i, out num, out attachmentParamState);
				if ((attachmentParamState & AttachmentParamState.UserInterface) != AttachmentParamState.Disabled)
				{
					AttachmentParam attachmentParam = (AttachmentParam)i;
					IAttachmentsParameterFormatter attachmentsParameterFormatter;
					string text3;
					bool flag;
					if (AttachmentParameterFormatters.Formatters.TryGetValue(attachmentParam, out attachmentsParameterFormatter) && attachmentsParameterFormatter.FormatParameter(attachmentParam, this.SelectedFirearm, attachmentId, num, out text3, out flag))
					{
						string text4;
						bool flag2 = TranslationReader.TryGet("AttachmentParameters", i, out text4);
						string text5 = "\n" + (flag2 ? text4 : attachmentParam.ToString()) + ": " + text3;
						if (flag)
						{
							TextMeshProUGUI pros = this._pros;
							pros.text += text5;
						}
						else
						{
							TextMeshProUGUI cons = this._cons;
							cons.text += text5;
						}
					}
				}
			}
			string text6;
			string text7;
			NonParameterFormatter.Format(this.SelectedFirearm, attachmentId, out text6, out text7);
			TextMeshProUGUI pros2 = this._pros;
			pros2.text += text6;
			TextMeshProUGUI cons2 = this._cons;
			cons2.text += text7;
		}

		protected abstract void LoadPreset(uint loadedCode);

		protected abstract void SelectAttachmentId(byte attachmentId);

		public abstract void RegisterAction(RectTransform t, Action<Vector2> action);

		public bool CanSaveAsPreference(int presetId)
		{
			if (presetId == 0 || this.SelectedFirearm == null)
			{
				return false;
			}
			uint currentAttachmentsCode = this.SelectedFirearm.GetCurrentAttachmentsCode();
			return AttachmentPreferences.GetPreferenceCodeOfPreset(this.SelectedFirearm.ItemTypeId, presetId) != currentAttachmentsCode;
		}

		public void SaveAsPreset(int presetId)
		{
			if (!this.CanSaveAsPreference(presetId))
			{
				return;
			}
			AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, presetId);
			this.SelectedFirearm.SavePreferenceCode();
			Action onPresetSaved = AttachmentSelectorBase.OnPresetSaved;
			if (onPresetSaved == null)
			{
				return;
			}
			onPresetSaved();
		}

		public void LoadPreset(int presetId)
		{
			AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, presetId);
			if (presetId != 0)
			{
				this.LoadPreset(this.SelectedFirearm.GetSavedPreferenceCode());
				Action onPresetLoaded = AttachmentSelectorBase.OnPresetLoaded;
				if (onPresetLoaded == null)
				{
					return;
				}
				onPresetLoaded();
			}
		}

		public void ResetAttachments()
		{
			AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, 0);
			this.LoadPreset(this.SelectedFirearm.ValidateAttachmentsCode(0U));
			Action onAttachmentsReset = AttachmentSelectorBase.OnAttachmentsReset;
			if (onAttachmentsReset == null)
			{
				return;
			}
			onAttachmentsReset();
		}

		public void ToggleSummaryScreen(bool summary)
		{
			this._summaryScreen.SetActive(summary);
			this._selectorScreen.SetActive(!summary);
			Action onSummaryToggled = this.OnSummaryToggled;
			if (onSummaryToggled == null)
			{
				return;
			}
			onSummaryToggled();
		}

		public void ToggleSummaryScreen()
		{
			this.ToggleSummaryScreen(!this._summaryScreen.activeSelf);
		}

		protected void LerpRects(float lerpState)
		{
			this._bodyImage.rectTransform.localScale = Vector3.Lerp(this._bodyImage.rectTransform.localScale, this._targetScale, lerpState);
			this._bodyImage.rectTransform.localPosition = Vector3.Lerp(this._bodyImage.rectTransform.localPosition, this._targetPosition, lerpState);
		}

		private void Lookat(Vector3 pos)
		{
			CameraShakeController.AddEffect(new LookatShake(pos));
		}

		private void DisableAllSelectableAttachments()
		{
			MonoBehaviour[] selectableAttachmentsPool = this.SelectableAttachmentsPool;
			for (int i = 0; i < selectableAttachmentsPool.Length; i++)
			{
				((IAttachmentSelectorButton)selectableAttachmentsPool[i]).RectTransform.gameObject.SetActive(false);
			}
		}

		protected bool RefreshState(Firearm firearm, byte? refreshReason)
		{
			this._spawnedConfigButtons.ForEach(delegate(GameObject x)
			{
				global::UnityEngine.Object.Destroy(x.gameObject);
			});
			this._spawnedConfigButtons.Clear();
			if (firearm != this.SelectedFirearm)
			{
				this.SelectedFirearm = firearm;
				this.SelectedSlot = AttachmentSlot.Unassigned;
				this._bodyImage.rectTransform.localScale = Vector3.zero;
				this.DisableAllSelectableAttachments();
				if (firearm == null)
				{
					return false;
				}
				if (this.SelectedFirearm.GetSavedPreferenceCode() != this.SelectedFirearm.GetCurrentAttachmentsCode())
				{
					AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, 0);
				}
			}
			if (firearm == null)
			{
				return false;
			}
			this._attachmentDimmer.alpha = Mathf.Clamp01(this._attachmentDimmer.alpha + Time.deltaTime * (this._isCorrectAttachment ? this._dimmerSpeed : (-this._dimmerSpeed)));
			this._bodyImage.texture = firearm.BodyIconTexture;
			this._bodyImage.rectTransform.sizeDelta = new Vector2((float)firearm.BodyIconTexture.width, (float)firearm.BodyIconTexture.height);
			int num = 0;
			for (int i = 0; i < firearm.Attachments.Length; i++)
			{
				IAttachmentSelectorButton component = this.SlotsPool[num].GetComponent<IAttachmentSelectorButton>();
				if (firearm.Attachments[i].IsEnabled)
				{
					IDisplayableAttachment displayableAttachment = firearm.Attachments[i] as IDisplayableAttachment;
					if (displayableAttachment != null)
					{
						Vector2 vector = displayableAttachment.IconOffset;
						if (firearm.Attachments[Mathf.Clamp(displayableAttachment.ParentId, 0, firearm.Attachments.Length)].IsEnabled)
						{
							vector += displayableAttachment.ParentOffset;
						}
						component.RectTransform.gameObject.SetActive(true);
						component.Setup(displayableAttachment.Icon, firearm.Attachments[i].Slot, new Vector2?(vector), firearm);
						component.ButtonId = (byte)(32 + firearm.Attachments[i].Slot);
						ICustomizableAttachment ica = displayableAttachment as ICustomizableAttachment;
						if (ica != null)
						{
							RectTransform rectTransform = global::UnityEngine.Object.Instantiate<RectTransform>(this._configIcon, component.RectTransform);
							rectTransform.localScale *= ica.ConfigIconScale / rectTransform.localScale.x;
							rectTransform.localRotation = Quaternion.Euler(AttachmentSelectorBase.SpinRotation);
							rectTransform.localPosition = ica.ConfigIconOffset;
							rectTransform.gameObject.SetActive(true);
							this._spawnedConfigButtons.Add(rectTransform.gameObject);
							Attachment att = firearm.Attachments[i];
							this.RegisterAction(rectTransform, delegate(Vector2 _)
							{
								RectTransform component2 = this._selectorScreen.GetComponent<RectTransform>();
								AttachmentConfigWindow attachmentConfigWindow = global::UnityEngine.Object.Instantiate<AttachmentConfigWindow>(ica.ConfigWindow, component2.parent);
								attachmentConfigWindow.Setup(this, att, component2);
								attachmentConfigWindow.OnDestroyed = (Action)Delegate.Combine(attachmentConfigWindow.OnDestroyed, new Action(delegate
								{
									this.ToggleSummaryScreen(false);
								}));
							});
						}
						num++;
						if (this.UseLookatMode)
						{
							this.LerpRects(1f);
							if (refreshReason != null && refreshReason.Value < 32 && firearm.Attachments[(int)refreshReason.Value].Slot == firearm.Attachments[i].Slot)
							{
								this.Lookat(component.RectTransform.transform.position);
							}
						}
					}
				}
			}
			for (int j = num; j < this.SlotsPool.Length; j++)
			{
				this.SlotsPool[j].gameObject.SetActive(false);
			}
			this.DisableAllSelectableAttachments();
			if (this.SelectedSlot == AttachmentSlot.Unassigned)
			{
				this.FitToRect(this._fullscreenRect);
			}
			else
			{
				int num2 = 0;
				float num3 = 0f;
				float num4 = 0f;
				Transform transform = null;
				byte b = 0;
				while ((int)b < firearm.Attachments.Length)
				{
					IAttachmentSelectorButton attachmentSelectorButton;
					if (firearm.Attachments[(int)b].Slot == this.SelectedSlot && this.SelectableAttachmentsPool[num2].TryGetComponent<IAttachmentSelectorButton>(out attachmentSelectorButton))
					{
						IDisplayableAttachment displayableAttachment2 = firearm.Attachments[(int)b] as IDisplayableAttachment;
						if (displayableAttachment2 != null)
						{
							attachmentSelectorButton.RectTransform.gameObject.SetActive(true);
							attachmentSelectorButton.ButtonId = b;
							attachmentSelectorButton.Setup(displayableAttachment2.Icon, AttachmentSlot.Unassigned, null, firearm);
							num4 = Mathf.Max(num4, attachmentSelectorButton.RectTransform.sizeDelta.y);
							num3 += attachmentSelectorButton.RectTransform.sizeDelta.x;
							if (this.UseLookatMode && refreshReason != null && refreshReason.Value - 32 == (byte)this.SelectedSlot && firearm.Attachments[(int)b].IsEnabled)
							{
								transform = attachmentSelectorButton.RectTransform.transform;
							}
							num2++;
						}
					}
					b += 1;
				}
				float num5 = Mathf.Max(1f / this._selectableMaxScale, Mathf.Max(num3 / this._selectableMaxWidth, num4 / this._selectableMaxHeight));
				foreach (IAttachmentSelectorButton attachmentSelectorButton2 in this.SelectableAttachmentsPool)
				{
					if (attachmentSelectorButton2.RectTransform.gameObject.activeSelf)
					{
						attachmentSelectorButton2.RectTransform.sizeDelta /= num5;
					}
				}
				this.FitToRect(this._sideRect);
				if (transform != null)
				{
					this._selectableLayoutGroup.SetLayoutVertical();
					this.LerpRects(1f);
					this.Lookat(transform.transform.position);
				}
			}
			return true;
		}

		private void FitToRect(RectTransform rt)
		{
			Vector3 localScale = this._bodyImage.rectTransform.localScale;
			Vector3 localPosition = this._bodyImage.rectTransform.localPosition;
			Bounds bounds = default(Bounds);
			this._bodyImage.rectTransform.localScale = Vector3.one;
			this._bodyImage.rectTransform.localPosition = Vector3.zero;
			this.Encapsulate(ref bounds, this._bodyImage.rectTransform);
			foreach (IAttachmentSelectorButton attachmentSelectorButton in this.SlotsPool)
			{
				if (attachmentSelectorButton.RectTransform.gameObject.activeSelf)
				{
					this.Encapsulate(ref bounds, attachmentSelectorButton.RectTransform);
				}
			}
			Vector3 vector = bounds.center - this._bodyImage.rectTransform.localPosition;
			float num = Mathf.Min(this._maxDisplayedScale, Mathf.Min(rt.sizeDelta.x / bounds.size.x, rt.sizeDelta.y / bounds.size.y));
			this._targetScale = Vector3.one * num;
			this._targetPosition = rt.localPosition - vector * num;
			this._bodyImage.rectTransform.localScale = localScale;
			this._bodyImage.rectTransform.localPosition = localPosition;
		}

		private void Encapsulate(ref Bounds b, RectTransform rct)
		{
			Vector2 vector = rct.sizeDelta / 2f;
			b.Encapsulate(rct.localPosition + Vector3.up * vector.y + Vector3.left * vector.x);
			b.Encapsulate(rct.localPosition + Vector3.down * vector.y + Vector3.right * vector.x);
		}

		public static Action OnPresetLoaded;

		public static Action OnPresetSaved;

		public static Action OnAttachmentsReset;

		public Action OnSummaryToggled;

		protected AttachmentSlot SelectedSlot = AttachmentSlot.Unassigned;

		[SerializeField]
		protected MonoBehaviour[] SlotsPool;

		[SerializeField]
		protected MonoBehaviour[] SelectableAttachmentsPool;

		[SerializeField]
		private TextMeshProUGUI _attachmentName;

		[SerializeField]
		private TextMeshProUGUI _attachmentDescription;

		[SerializeField]
		private TextMeshProUGUI _pros;

		[SerializeField]
		private TextMeshProUGUI _cons;

		[SerializeField]
		private CanvasGroup _attachmentDimmer;

		[SerializeField]
		private float _dimmerSpeed;

		[SerializeField]
		private RawImage _bodyImage;

		[SerializeField]
		private RectTransform _fullscreenRect;

		[SerializeField]
		private RectTransform _sideRect;

		[SerializeField]
		private RectTransform _selectableRect;

		[SerializeField]
		private GameObject _selectorScreen;

		[SerializeField]
		private GameObject _summaryScreen;

		[SerializeField]
		private float _selectableMaxHeight;

		[SerializeField]
		private float _selectableMaxWidth;

		[SerializeField]
		private float _selectableMaxScale;

		[SerializeField]
		private float _maxDisplayedScale = 2f;

		[SerializeField]
		private RectTransform _configIcon;

		[SerializeField]
		private HorizontalOrVerticalLayoutGroup _selectableLayoutGroup;

		private Vector3 _targetScale;

		private Vector3 _targetPosition;

		private bool _isCorrectAttachment;

		private readonly List<GameObject> _spawnedConfigButtons = new List<GameObject>();

		private const byte SlotOffset = 32;

		private const string AttParamsFilename = "AttachmentParameters";
	}
}

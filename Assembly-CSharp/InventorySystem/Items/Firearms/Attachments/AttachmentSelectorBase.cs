using System;
using System.Collections.Generic;
using CameraShaking;
using InventorySystem.Items.Firearms.Attachments.Components;
using InventorySystem.Items.Firearms.Attachments.Formatters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem.Items.Firearms.Attachments;

public abstract class AttachmentSelectorBase : MonoBehaviour
{
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

	public Firearm SelectedFirearm { get; protected set; }

	protected abstract bool UseLookatMode { get; set; }

	private static Vector3 SpinRotation => Vector3.back * Time.timeSinceLevelLoad * 100f;

	public void ProcessCollider(byte colId)
	{
		if (this.SelectedFirearm == null)
		{
			return;
		}
		this.RefreshState(this.SelectedFirearm, colId);
		if (colId >= 32)
		{
			this.SelectedSlot = (AttachmentSlot)(colId - 32);
			if (this.UseLookatMode)
			{
				this.RefreshState(this.SelectedFirearm, colId);
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
		attachment.GetNameAndDescription(out var n, out var d);
		this._pros.text = string.Empty;
		this._cons.text = string.Empty;
		this._attachmentName.text = n;
		this._attachmentDescription.text = d;
		for (int i = 0; i < AttachmentsUtils.TotalNumberOfParams; i++)
		{
			attachment.GetParameterData(i, out var val, out var state);
			if ((state & AttachmentParamState.UserInterface) == 0)
			{
				continue;
			}
			AttachmentParam attachmentParam = (AttachmentParam)i;
			if (AttachmentParameterFormatters.Formatters.TryGetValue(attachmentParam, out var value) && value.FormatParameter(attachmentParam, this.SelectedFirearm, attachmentId, val, out var formattedText, out var isGood))
			{
				string val2;
				bool flag = TranslationReader.TryGet("AttachmentParameters", i, out val2);
				string text = "\n" + (flag ? val2 : attachmentParam.ToString()) + ": " + formattedText;
				if (isGood)
				{
					this._pros.text += text;
				}
				else
				{
					this._cons.text += text;
				}
			}
		}
		NonParameterFormatter.Format(this.SelectedFirearm, attachmentId, out var pros, out var cons);
		this._pros.text += pros;
		this._cons.text += cons;
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
		if (this.CanSaveAsPreference(presetId))
		{
			AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, presetId);
			this.SelectedFirearm.SavePreferenceCode();
			AttachmentSelectorBase.OnPresetSaved?.Invoke();
		}
	}

	public void LoadPreset(int presetId)
	{
		AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, presetId);
		if (presetId != 0)
		{
			this.LoadPreset(this.SelectedFirearm.GetSavedPreferenceCode());
			AttachmentSelectorBase.OnPresetLoaded?.Invoke();
		}
	}

	public void ResetAttachments()
	{
		AttachmentPreferences.SetPreset(this.SelectedFirearm.ItemTypeId, 0);
		this.LoadPreset(this.SelectedFirearm.ValidateAttachmentsCode(0u));
		AttachmentSelectorBase.OnAttachmentsReset?.Invoke();
	}

	public void ToggleSummaryScreen(bool summary)
	{
		this._summaryScreen.SetActive(summary);
		this._selectorScreen.SetActive(!summary);
		this.OnSummaryToggled?.Invoke();
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
			((IAttachmentSelectorButton)selectableAttachmentsPool[i]).RectTransform.gameObject.SetActive(value: false);
		}
	}

	protected bool RefreshState(Firearm firearm, byte? refreshReason)
	{
		this._spawnedConfigButtons.ForEach(delegate(GameObject x)
		{
			UnityEngine.Object.Destroy(x.gameObject);
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
		this._attachmentDimmer.alpha = Mathf.Clamp01(this._attachmentDimmer.alpha + Time.deltaTime * (this._isCorrectAttachment ? this._dimmerSpeed : (0f - this._dimmerSpeed)));
		this._bodyImage.texture = firearm.BodyIconTexture;
		this._bodyImage.rectTransform.sizeDelta = new Vector2(firearm.BodyIconTexture.width, firearm.BodyIconTexture.height);
		int num = 0;
		for (int num2 = 0; num2 < firearm.Attachments.Length; num2++)
		{
			IAttachmentSelectorButton component = this.SlotsPool[num].GetComponent<IAttachmentSelectorButton>();
			if (!firearm.Attachments[num2].IsEnabled || !(firearm.Attachments[num2] is IDisplayableAttachment { IconOffset: var vector } displayableAttachment))
			{
				continue;
			}
			if (firearm.Attachments[Mathf.Clamp(displayableAttachment.ParentId, 0, firearm.Attachments.Length)].IsEnabled)
			{
				vector += displayableAttachment.ParentOffset;
			}
			component.RectTransform.gameObject.SetActive(value: true);
			component.Setup(displayableAttachment.Icon, firearm.Attachments[num2].Slot, vector, firearm);
			component.ButtonId = (byte)(32 + firearm.Attachments[num2].Slot);
			ICustomizableAttachment ica = displayableAttachment as ICustomizableAttachment;
			if (ica != null)
			{
				RectTransform rectTransform = UnityEngine.Object.Instantiate(this._configIcon, component.RectTransform);
				rectTransform.localScale *= ica.ConfigIconScale / rectTransform.localScale.x;
				rectTransform.localRotation = Quaternion.Euler(AttachmentSelectorBase.SpinRotation);
				rectTransform.localPosition = ica.ConfigIconOffset;
				rectTransform.gameObject.SetActive(value: true);
				this._spawnedConfigButtons.Add(rectTransform.gameObject);
				Attachment att = firearm.Attachments[num2];
				this.RegisterAction(rectTransform, delegate
				{
					RectTransform component3 = this._selectorScreen.GetComponent<RectTransform>();
					AttachmentConfigWindow attachmentConfigWindow = UnityEngine.Object.Instantiate(ica.ConfigWindow, component3.parent);
					attachmentConfigWindow.Setup(this, att, component3);
					attachmentConfigWindow.OnDestroyed = (Action)Delegate.Combine(attachmentConfigWindow.OnDestroyed, (Action)delegate
					{
						this.ToggleSummaryScreen(summary: false);
					});
				});
			}
			num++;
			if (this.UseLookatMode)
			{
				this.LerpRects(1f);
				if (refreshReason.HasValue && refreshReason.Value < 32 && firearm.Attachments[refreshReason.Value].Slot == firearm.Attachments[num2].Slot)
				{
					this.Lookat(component.RectTransform.transform.position);
				}
			}
		}
		for (int num3 = num; num3 < this.SlotsPool.Length; num3++)
		{
			this.SlotsPool[num3].gameObject.SetActive(value: false);
		}
		this.DisableAllSelectableAttachments();
		if (this.SelectedSlot == AttachmentSlot.Unassigned)
		{
			this.FitToRect(this._fullscreenRect);
		}
		else
		{
			int num4 = 0;
			float num5 = 0f;
			float num6 = 0f;
			Transform transform = null;
			for (byte b = 0; b < firearm.Attachments.Length; b++)
			{
				if (firearm.Attachments[b].Slot == this.SelectedSlot && this.SelectableAttachmentsPool[num4].TryGetComponent<IAttachmentSelectorButton>(out var component2) && firearm.Attachments[b] is IDisplayableAttachment displayableAttachment2)
				{
					component2.RectTransform.gameObject.SetActive(value: true);
					component2.ButtonId = b;
					component2.Setup(displayableAttachment2.Icon, AttachmentSlot.Unassigned, null, firearm);
					num6 = Mathf.Max(num6, component2.RectTransform.sizeDelta.y);
					num5 += component2.RectTransform.sizeDelta.x;
					if (this.UseLookatMode && refreshReason.HasValue && refreshReason.Value - 32 == (int)this.SelectedSlot && firearm.Attachments[b].IsEnabled)
					{
						transform = component2.RectTransform.transform;
					}
					num4++;
				}
			}
			float num7 = Mathf.Max(1f / this._selectableMaxScale, Mathf.Max(num5 / this._selectableMaxWidth, num6 / this._selectableMaxHeight));
			MonoBehaviour[] selectableAttachmentsPool = this.SelectableAttachmentsPool;
			for (int num8 = 0; num8 < selectableAttachmentsPool.Length; num8++)
			{
				IAttachmentSelectorButton attachmentSelectorButton = (IAttachmentSelectorButton)selectableAttachmentsPool[num8];
				if (attachmentSelectorButton.RectTransform.gameObject.activeSelf)
				{
					attachmentSelectorButton.RectTransform.sizeDelta /= num7;
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
		Bounds b = default(Bounds);
		this._bodyImage.rectTransform.localScale = Vector3.one;
		this._bodyImage.rectTransform.localPosition = Vector3.zero;
		this.Encapsulate(ref b, this._bodyImage.rectTransform);
		MonoBehaviour[] slotsPool = this.SlotsPool;
		for (int i = 0; i < slotsPool.Length; i++)
		{
			IAttachmentSelectorButton attachmentSelectorButton = (IAttachmentSelectorButton)slotsPool[i];
			if (attachmentSelectorButton.RectTransform.gameObject.activeSelf)
			{
				this.Encapsulate(ref b, attachmentSelectorButton.RectTransform);
			}
		}
		Vector3 vector = b.center - this._bodyImage.rectTransform.localPosition;
		float num = Mathf.Min(this._maxDisplayedScale, Mathf.Min(rt.sizeDelta.x / b.size.x, rt.sizeDelta.y / b.size.y));
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
}

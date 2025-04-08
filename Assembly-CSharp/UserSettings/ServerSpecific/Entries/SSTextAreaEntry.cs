using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSTextAreaEntry : MonoBehaviour, ISSEntry
	{
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSTextArea;
		}

		public void Init(ServerSpecificSettingBase setting)
		{
			this._textArea = setting as SSTextArea;
			this._mainText.text = this._textArea.Label;
			this._mainText.alignment = this._textArea.AlignmentOptions;
			this._foldoutMode = this._textArea.Foldout;
			this.UpdateText();
			switch (this._foldoutMode)
			{
			case SSTextArea.FoldoutMode.NotCollapsable:
				this._toggle.isOn = true;
				this._mainText.raycastTarget = true;
				this._foldoutGroup.ExtendInstantly();
				this._disableWhenNotCollapsable.ForEach(delegate(GameObject x)
				{
					x.SetActive(false);
				});
				break;
			case SSTextArea.FoldoutMode.CollapseOnEntry:
			case SSTextArea.FoldoutMode.CollapsedByDefault:
				this._foldoutGroup.FoldInstantly();
				break;
			case SSTextArea.FoldoutMode.ExtendOnEntry:
			case SSTextArea.FoldoutMode.ExtendedByDefault:
				this._foldoutGroup.ExtendInstantly();
				break;
			}
			this._textArea.OnTextUpdated += this.UpdateText;
		}

		private void UpdateText()
		{
			this._mainText.SetText(this._textArea.Label);
			this._foldoutGroup.RefreshSize();
			if (this._foldoutMode == SSTextArea.FoldoutMode.NotCollapsable)
			{
				return;
			}
			this._shortText.text = (string.IsNullOrEmpty(this._textArea.HintDescription) ? this._textArea.Label : this._textArea.HintDescription);
		}

		private void OnDestroy()
		{
			if (this._textArea == null)
			{
				return;
			}
			this._textArea.OnTextUpdated -= this.UpdateText;
		}

		private void OnDisable()
		{
			SSTextArea.FoldoutMode foldoutMode = this._foldoutMode;
			if (foldoutMode == SSTextArea.FoldoutMode.CollapseOnEntry)
			{
				this._foldoutGroup.FoldInstantly();
				return;
			}
			if (foldoutMode != SSTextArea.FoldoutMode.ExtendOnEntry)
			{
				return;
			}
			this._foldoutGroup.ExtendInstantly();
		}

		private void Update()
		{
			if (this._foldoutMode == SSTextArea.FoldoutMode.NotCollapsable)
			{
				return;
			}
			this._shortText.alpha = 1f - this._group.alpha;
		}

		[SerializeField]
		private TMP_Text _shortText;

		[SerializeField]
		private TMP_Text _mainText;

		[SerializeField]
		private CanvasGroup _group;

		[SerializeField]
		private Toggle _toggle;

		[SerializeField]
		private UserSettingsFoldoutGroup _foldoutGroup;

		[SerializeField]
		private GameObject[] _disableWhenNotCollapsable;

		private SSTextArea.FoldoutMode _foldoutMode;

		private SSTextArea _textArea;
	}
}

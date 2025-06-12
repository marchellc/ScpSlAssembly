using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UserSettings.GUIElements;

namespace UserSettings.ServerSpecific.Entries;

public class SSTextAreaEntry : MonoBehaviour, ISSEntry
{
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
		case SSTextArea.FoldoutMode.CollapseOnEntry:
		case SSTextArea.FoldoutMode.CollapsedByDefault:
			this._foldoutGroup.FoldInstantly();
			break;
		case SSTextArea.FoldoutMode.ExtendOnEntry:
		case SSTextArea.FoldoutMode.ExtendedByDefault:
			this._foldoutGroup.ExtendInstantly();
			break;
		case SSTextArea.FoldoutMode.NotCollapsable:
			this._toggle.isOn = true;
			this._mainText.raycastTarget = true;
			this._foldoutGroup.ExtendInstantly();
			this._disableWhenNotCollapsable.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			break;
		}
		this._textArea.OnTextUpdated += UpdateText;
	}

	private void UpdateText()
	{
		this._mainText.SetText(this._textArea.Label);
		this._foldoutGroup.RefreshSize();
		if (this._foldoutMode != SSTextArea.FoldoutMode.NotCollapsable)
		{
			this._shortText.text = (string.IsNullOrEmpty(this._textArea.HintDescription) ? this._textArea.Label : this._textArea.HintDescription);
		}
	}

	private void OnDestroy()
	{
		if (this._textArea != null)
		{
			this._textArea.OnTextUpdated -= UpdateText;
		}
	}

	private void OnDisable()
	{
		switch (this._foldoutMode)
		{
		case SSTextArea.FoldoutMode.CollapseOnEntry:
			this._foldoutGroup.FoldInstantly();
			break;
		case SSTextArea.FoldoutMode.ExtendOnEntry:
			this._foldoutGroup.ExtendInstantly();
			break;
		}
	}

	private void Update()
	{
		if (this._foldoutMode != SSTextArea.FoldoutMode.NotCollapsable)
		{
			this._shortText.alpha = 1f - this._group.alpha;
		}
	}
}

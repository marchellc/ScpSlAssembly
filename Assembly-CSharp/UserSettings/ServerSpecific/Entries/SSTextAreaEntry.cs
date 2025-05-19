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
		_textArea = setting as SSTextArea;
		_mainText.text = _textArea.Label;
		_mainText.alignment = _textArea.AlignmentOptions;
		_foldoutMode = _textArea.Foldout;
		UpdateText();
		switch (_foldoutMode)
		{
		case SSTextArea.FoldoutMode.CollapseOnEntry:
		case SSTextArea.FoldoutMode.CollapsedByDefault:
			_foldoutGroup.FoldInstantly();
			break;
		case SSTextArea.FoldoutMode.ExtendOnEntry:
		case SSTextArea.FoldoutMode.ExtendedByDefault:
			_foldoutGroup.ExtendInstantly();
			break;
		case SSTextArea.FoldoutMode.NotCollapsable:
			_toggle.isOn = true;
			_mainText.raycastTarget = true;
			_foldoutGroup.ExtendInstantly();
			_disableWhenNotCollapsable.ForEach(delegate(GameObject x)
			{
				x.SetActive(value: false);
			});
			break;
		}
		_textArea.OnTextUpdated += UpdateText;
	}

	private void UpdateText()
	{
		_mainText.SetText(_textArea.Label);
		_foldoutGroup.RefreshSize();
		if (_foldoutMode != 0)
		{
			_shortText.text = (string.IsNullOrEmpty(_textArea.HintDescription) ? _textArea.Label : _textArea.HintDescription);
		}
	}

	private void OnDestroy()
	{
		if (_textArea != null)
		{
			_textArea.OnTextUpdated -= UpdateText;
		}
	}

	private void OnDisable()
	{
		switch (_foldoutMode)
		{
		case SSTextArea.FoldoutMode.CollapseOnEntry:
			_foldoutGroup.FoldInstantly();
			break;
		case SSTextArea.FoldoutMode.ExtendOnEntry:
			_foldoutGroup.ExtendInstantly();
			break;
		}
	}

	private void Update()
	{
		if (_foldoutMode != 0)
		{
			_shortText.alpha = 1f - _group.alpha;
		}
	}
}

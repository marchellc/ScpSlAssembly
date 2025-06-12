using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.RoleHelp;

public class KeyActionIcon : MonoBehaviour
{
	[SerializeField]
	private ActionName _action;

	[SerializeField]
	private Image _keycodeBg;

	[SerializeField]
	private TMP_Text _keycodeText;

	[SerializeField]
	private TMP_Text _mouseText;

	[SerializeField]
	private GameObject[] _mouseIcons;

	private const KeyCode MouseMin = KeyCode.Mouse0;

	private const KeyCode MouseMax = KeyCode.Mouse6;

	private void Awake()
	{
		this.Refresh();
		NewInput.OnKeyModified += OnKeyModified;
	}

	private void OnDestroy()
	{
		NewInput.OnKeyModified -= OnKeyModified;
	}

	private void OnKeyModified(ActionName actionName, KeyCode kc)
	{
		if (actionName == this._action)
		{
			this.Refresh(kc);
		}
	}

	private void Refresh()
	{
		this.Refresh(NewInput.GetKey(this._action));
	}

	private void Refresh(KeyCode kc)
	{
		this._mouseIcons.ForEach(delegate(GameObject x)
		{
			x.SetActive(value: false);
		});
		if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6)
		{
			this.HandleMouse((int)(kc - 323));
		}
		else
		{
			this.HandleKeycode(new ReadableKeyCode(kc));
		}
	}

	private void HandleMouse(int buttonId)
	{
		this._keycodeText.text = string.Empty;
		this._mouseText.text = buttonId.ToString();
		int b = this._mouseIcons.Length - 1;
		int num = Mathf.Min(buttonId, b);
		this._mouseIcons[num].SetActive(value: true);
		this._keycodeBg.enabled = false;
	}

	private void HandleKeycode(ReadableKeyCode rkc)
	{
		this._keycodeText.text = rkc.NormalVersion;
		this._keycodeBg.enabled = true;
	}
}

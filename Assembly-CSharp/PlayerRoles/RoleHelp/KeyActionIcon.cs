using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.RoleHelp
{
	public class KeyActionIcon : MonoBehaviour
	{
		private void Awake()
		{
			this.Refresh();
			NewInput.OnKeyModified += this.OnKeyModified;
		}

		private void OnDestroy()
		{
			NewInput.OnKeyModified -= this.OnKeyModified;
		}

		private void OnKeyModified(ActionName actionName, KeyCode kc)
		{
			if (actionName != this._action)
			{
				return;
			}
			this.Refresh(kc);
		}

		private void Refresh()
		{
			this.Refresh(NewInput.GetKey(this._action, KeyCode.None));
		}

		private void Refresh(KeyCode kc)
		{
			this._mouseIcons.ForEach(delegate(GameObject x)
			{
				x.SetActive(false);
			});
			if (kc >= KeyCode.Mouse0 && kc <= KeyCode.Mouse6)
			{
				this.HandleMouse(kc - KeyCode.Mouse0);
				return;
			}
			this.HandleKeycode(new ReadableKeyCode(kc));
		}

		private void HandleMouse(int buttonId)
		{
			this._keycodeText.text = string.Empty;
			this._mouseText.text = buttonId.ToString();
			int num = this._mouseIcons.Length - 1;
			int num2 = Mathf.Min(buttonId, num);
			this._mouseIcons[num2].SetActive(true);
			this._keycodeBg.enabled = false;
		}

		private void HandleKeycode(ReadableKeyCode rkc)
		{
			this._keycodeText.text = rkc.NormalVersion;
			this._keycodeBg.enabled = true;
		}

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
	}
}

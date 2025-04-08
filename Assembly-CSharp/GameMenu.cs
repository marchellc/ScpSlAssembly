using System;
using System.Collections.Generic;
using MEC;
using Mirror;
using NorthwoodLib;
using TMPro;
using ToggleableMenus;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameMenu : SimpleToggleableMenu
{
	public override bool LockMovement
	{
		get
		{
			GameObject currentSelectedGameObject = EventSystem.current.currentSelectedGameObject;
			TMP_InputField tmp_InputField;
			return currentSelectedGameObject != null && currentSelectedGameObject.TryGetComponent<TMP_InputField>(out tmp_InputField) && tmp_InputField.isFocused;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		GameMenu.singleton = this;
	}

	private void Update()
	{
		if (HideHUDController.IsHUDVisible)
		{
			return;
		}
		this.hideHUDText.GetComponent<TextMeshProUGUI>().text = "Warning: HUD is hidden\n" + string.Format("Press <b>{0}</b> to enable HUD", NewInput.GetKey(ActionName.HideGUI, KeyCode.None));
	}

	private IEnumerator<float> _ShowServerInfo(string id)
	{
		this.infoText.text = "";
		if (id.Contains("/"))
		{
			this.infoText.text = "The URL isn't directing to pastebin site. Please contact server owner.";
		}
		else
		{
			using (UnityWebRequest www = UnityWebRequest.Get("https://pastebin.com/raw/" + id))
			{
				yield return Timing.WaitUntilDone(www.SendWebRequest());
				string text = (string.IsNullOrEmpty(www.error) ? www.downloadHandler.text : www.error);
				if (text.Contains("<title>Pastebin.com - Locked Paste</title>", StringComparison.OrdinalIgnoreCase))
				{
					this.infoText.text = "The provided paste is locked via password and cannot be displayed. Please contact the server owner.";
					yield break;
				}
				if (text.Length > 5000)
				{
					text = text.TruncateToLast(5000, '\n');
					text = text + "...\n<i><color=#87CEFA><u><link=\"https://pastebin.com/" + id + "\">(Click here for full content)</link></u></color></i>";
				}
				this.infoText.text = text;
				this._pastebinDisplayed = true;
			}
			UnityWebRequest www = null;
		}
		yield break;
		yield break;
	}

	protected override void OnToggled()
	{
		base.OnToggled();
		foreach (GameObject gameObject in this.minors)
		{
			if (gameObject.activeSelf)
			{
				gameObject.SetActive(false);
			}
		}
	}

	public void SelectMinor(int id)
	{
		foreach (GameObject gameObject in this.minors)
		{
			if (gameObject.activeSelf)
			{
				gameObject.SetActive(false);
			}
		}
		this.minors[id].SetActive(true);
		ReferenceHub referenceHub;
		if (id == 2 && !this._pastebinDisplayed && ReferenceHub.TryGetHostHub(out referenceHub))
		{
			Timing.RunCoroutine(this._ShowServerInfo(referenceHub.characterClassManager.Pastebin), Segment.FixedUpdate);
		}
	}

	public void Disconnect()
	{
		if (NetworkServer.active)
		{
			NetworkManager.singleton.StopHost();
			return;
		}
		NetworkManager.singleton.StopClient();
	}

	public void Exit()
	{
		this.IsEnabled = false;
	}

	public static GameMenu singleton;

	public GameObject[] minors;

	public Graphic[] colorableElements;

	public TextMeshProUGUI infoText;

	public GameObject hideHUDText;

	private bool _pastebinDisplayed;
}

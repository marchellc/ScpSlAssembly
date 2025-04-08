using System;
using GameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HideHUDController : MonoBehaviour
{
	public static event Action<bool> ToggleHUD;

	public static bool IsHUDVisible
	{
		get
		{
			return HideHUDController._singleton == null || HideHUDController._singleton._showHUDElements;
		}
	}

	private void Awake()
	{
		HideHUDController._singleton = this;
	}

	private void Update()
	{
		if (RoundStart.singleton == null || !RoundStart.RoundStarted || !Input.GetKeyDown(NewInput.GetKey(ActionName.HideGUI, KeyCode.None)))
		{
			return;
		}
		if (this._showHUDElements)
		{
			InputField[] array = global::UnityEngine.Object.FindObjectsOfType<InputField>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].isFocused)
				{
					return;
				}
			}
			TMP_InputField[] array2 = global::UnityEngine.Object.FindObjectsOfType<TMP_InputField>();
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i].isFocused)
				{
					return;
				}
			}
		}
		this._showHUDElements = !this._showHUDElements;
		Action<bool> toggleHUD = HideHUDController.ToggleHUD;
		if (toggleHUD != null)
		{
			toggleHUD(this._showHUDElements);
		}
		GameMenu.singleton.hideHUDText.SetActive(!this._showHUDElements);
	}

	private static HideHUDController _singleton;

	private bool _showHUDElements = true;
}

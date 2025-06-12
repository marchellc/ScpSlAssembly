using System;
using GameCore;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HideHUDController : MonoBehaviour
{
	private static HideHUDController _singleton;

	private bool _showHUDElements = true;

	public static bool IsHUDVisible
	{
		get
		{
			if (!(HideHUDController._singleton == null))
			{
				return HideHUDController._singleton._showHUDElements;
			}
			return true;
		}
	}

	public static event Action<bool> ToggleHUD;

	private void Awake()
	{
		HideHUDController._singleton = this;
	}

	private void Update()
	{
		if (RoundStart.singleton == null || !RoundStart.RoundStarted || !Input.GetKeyDown(NewInput.GetKey(ActionName.HideGUI)))
		{
			return;
		}
		if (this._showHUDElements)
		{
			InputField[] array = UnityEngine.Object.FindObjectsOfType<InputField>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].isFocused)
				{
					return;
				}
			}
			TMP_InputField[] array2 = UnityEngine.Object.FindObjectsOfType<TMP_InputField>();
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i].isFocused)
				{
					return;
				}
			}
		}
		this._showHUDElements = !this._showHUDElements;
		HideHUDController.ToggleHUD?.Invoke(this._showHUDElements);
		GameMenu.singleton.hideHUDText.SetActive(!this._showHUDElements);
	}
}

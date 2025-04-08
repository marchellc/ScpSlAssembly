using System;
using UnityEngine;

public class NewMainMenu : MonoBehaviour
{
	public void QuitGame()
	{
		Shutdown.Quit(true, false);
	}

	public void Refresh()
	{
		SimpleMenu.LoadCorrectScene();
	}

	private void Start()
	{
	}
}

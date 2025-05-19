using UnityEngine;

public class NewMainMenu : MonoBehaviour
{
	public void QuitGame()
	{
		Shutdown.Quit();
	}

	public void Refresh()
	{
		SimpleMenu.LoadCorrectScene();
	}

	private void Start()
	{
	}
}

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
	private void Start()
	{
		if (SceneManager.GetActiveScene().name == "Facility")
		{
			Object.Destroy(base.gameObject);
		}
	}
}

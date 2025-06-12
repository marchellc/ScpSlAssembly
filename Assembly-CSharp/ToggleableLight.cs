using UnityEngine;

public class ToggleableLight : MonoBehaviour
{
	public GameObject[] allLights;

	public bool isAlarm;

	public void SetLights(bool b)
	{
		GameObject[] array = this.allLights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(this.isAlarm ? b : (!b));
		}
	}
}

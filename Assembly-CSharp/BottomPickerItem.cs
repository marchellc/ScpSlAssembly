using UnityEngine;

public class BottomPickerItem : MonoBehaviour
{
	private string key;

	private int id;

	public void SetupButton(string k, int i)
	{
		this.key = k;
		this.id = i;
	}

	public void Submit()
	{
		PlayerPrefsSl.Set(this.key, this.id);
	}
}

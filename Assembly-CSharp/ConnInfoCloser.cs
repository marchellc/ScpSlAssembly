using System;
using UnityEngine;

public class ConnInfoCloser : ConnInfoButton
{
	public override void UseButton()
	{
		GameObject[] array = this.objToClose;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(false);
		}
		base.UseButton();
	}

	public GameObject[] objToClose;
}

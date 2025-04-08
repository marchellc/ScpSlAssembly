using System;
using UnityEngine;

public class ConnInfoCanceler : ConnInfoButton
{
	public override void UseButton()
	{
		base.UseButton();
		global::UnityEngine.Object.FindObjectOfType<CustomNetworkManager>().StopClient();
	}
}

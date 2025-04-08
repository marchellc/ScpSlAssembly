using System;
using Mirror;
using UnityEngine;

public class FastRoundRestartController : NetworkBehaviour
{
	private void Start()
	{
		global::UnityEngine.Object.Destroy(this);
	}

	public override bool Weaved()
	{
		return true;
	}
}

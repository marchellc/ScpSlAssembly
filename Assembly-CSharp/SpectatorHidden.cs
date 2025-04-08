using System;
using GameObjectPools;
using UnityEngine;
using UnityEngine.Rendering;

public class SpectatorHidden : MonoBehaviour, IPoolResettable
{
	public Renderer AttachedRenderer { get; private set; }

	private void Awake()
	{
		this.AttachedRenderer = base.GetComponent<Renderer>();
	}

	public void ResetObject()
	{
		this.AttachedRenderer.shadowCastingMode = ShadowCastingMode.On;
	}
}

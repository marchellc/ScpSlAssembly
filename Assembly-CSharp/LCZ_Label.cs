using System.Collections.Generic;
using UnityEngine;

public class LCZ_Label : MonoBehaviour
{
	public static readonly HashSet<LCZ_Label> AllLabels = new HashSet<LCZ_Label>();

	public MeshRenderer chRend;

	public MeshRenderer numRend;

	private void Awake()
	{
		LCZ_Label.AllLabels.Add(this);
	}

	private void OnDestroy()
	{
		LCZ_Label.AllLabels.Remove(this);
	}

	public void Refresh(Material ch, Material num)
	{
		this.chRend.sharedMaterial = ch;
		this.numRend.sharedMaterial = num;
	}
}

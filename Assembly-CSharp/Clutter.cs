using System;
using System.Collections.Generic;
using GameCore;
using UnityEngine;

public class Clutter : MonoBehaviour
{
	public void SpawnClutter()
	{
		global::GameCore.Console.AddDebugLog("MGCLTR", "Spawning clutter component on object of name \"" + base.gameObject.name + "\"", MessageImportance.LeastImportant, true);
		this.spawned = true;
		if (!this.holderObject)
		{
			this.holderObject = base.gameObject;
		}
		GameObject gameObject = global::UnityEngine.Object.Instantiate<GameObject>((this.possiblePrefabs.Count > 0) ? this.possiblePrefabs[global::UnityEngine.Random.Range(0, this.possiblePrefabs.Count)] : base.gameObject, this.holderObject.transform.position + this.spawnOffset * 0.72745f, this.holderObject.transform.rotation.normalized, this.holderObject.transform.parent);
		if (this.clutterScale != Vector3.zero)
		{
			gameObject.transform.localScale = this.clutterScale;
		}
		else
		{
			gameObject.transform.localScale = this.holderObject.transform.localScale;
		}
		gameObject.SetActive(true);
		Clutter clutter;
		if (gameObject.TryGetComponent<Clutter>(out clutter))
		{
			global::UnityEngine.Object.Destroy(clutter);
		}
		global::UnityEngine.Object.Destroy(this.holderObject);
	}

	[Header("Prefab Data")]
	public GameObject holderObject;

	public List<GameObject> possiblePrefabs = new List<GameObject>();

	public Vector3 spawnOffset;

	public Vector3 clutterScale = Vector3.zero;

	public bool spawned;

	private const float OverallScale = 0.72745f;
}

using System.Collections.Generic;
using GameCore;
using UnityEngine;

public class Clutter : MonoBehaviour
{
	[Header("Prefab Data")]
	public GameObject holderObject;

	public List<GameObject> possiblePrefabs = new List<GameObject>();

	public Vector3 spawnOffset;

	public Vector3 clutterScale = Vector3.zero;

	public bool spawned;

	private const float OverallScale = 0.72745f;

	public void SpawnClutter()
	{
		Console.AddDebugLog("MGCLTR", "Spawning clutter component on object of name \"" + base.gameObject.name + "\"", MessageImportance.LeastImportant, nospace: true);
		this.spawned = true;
		if (!this.holderObject)
		{
			this.holderObject = base.gameObject;
		}
		GameObject gameObject = Object.Instantiate((this.possiblePrefabs.Count > 0) ? this.possiblePrefabs[Random.Range(0, this.possiblePrefabs.Count)] : base.gameObject, this.holderObject.transform.position + this.spawnOffset * 0.72745f, this.holderObject.transform.rotation.normalized, this.holderObject.transform.parent);
		if (this.clutterScale != Vector3.zero)
		{
			gameObject.transform.localScale = this.clutterScale;
		}
		else
		{
			gameObject.transform.localScale = this.holderObject.transform.localScale;
		}
		gameObject.SetActive(value: true);
		if (gameObject.TryGetComponent<Clutter>(out var component))
		{
			Object.Destroy(component);
		}
		Object.Destroy(this.holderObject);
	}
}

using UnityEngine;

namespace MapGeneration.StaticHelpers;

public class DynamicChildController : MonoBehaviour
{
	private static Transform _dynamicRoot;

	[SerializeField]
	private GameObject spawnPrefab;

	public GameObject Child { get; private set; }

	public void SetChild(GameObject child)
	{
		child.SetActive(child.activeInHierarchy);
		child.transform.GetPositionAndRotation(out var position, out var rotation);
		child.transform.parent = ((DynamicChildController._dynamicRoot == null) ? DynamicChildController.CreateDynamicRoot() : DynamicChildController._dynamicRoot);
		child.transform.SetPositionAndRotation(position, rotation);
		this.Child = child;
	}

	private static Transform CreateDynamicRoot()
	{
		DynamicChildController._dynamicRoot = new GameObject("DynamicChild Container").transform;
		return DynamicChildController._dynamicRoot;
	}

	private void Awake()
	{
		if (!(this.spawnPrefab == null))
		{
			this.SetChild(Object.Instantiate(this.spawnPrefab, base.transform.position, base.transform.rotation, base.transform));
		}
	}

	private void OnEnable()
	{
		if (!(this.Child == null))
		{
			this.Child.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		if (!(this.Child == null))
		{
			this.Child.SetActive(value: false);
		}
	}

	private void OnDestroy()
	{
		Object.Destroy(this.Child);
	}
}

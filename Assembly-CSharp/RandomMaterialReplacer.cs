using UnityEngine;

public class RandomMaterialReplacer : MonoBehaviour
{
	public Material[] mats;

	private void Start()
	{
		int num = Random.Range(0, this.mats.Length);
		MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].material = this.mats[num];
		}
	}
}

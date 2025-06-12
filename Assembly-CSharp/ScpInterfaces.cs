using UnityEngine;

public class ScpInterfaces : MonoBehaviour
{
	public static ScpInterfaces singleton;

	public GameObject Scp106_eq;

	public GameObject Scp049_eq;

	public GameObject Scp096_eq;

	public GameObject Scp173InterfaceObj;

	public static int remTargs;

	private void Awake()
	{
		ScpInterfaces.singleton = this;
	}
}

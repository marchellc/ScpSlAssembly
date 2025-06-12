using UnityEngine;

public class DetectorBlink : MonoBehaviour
{
	public Material mat;

	private bool state;

	private void Start()
	{
		this.Blink();
	}

	private void Blink()
	{
		this.state = !this.state;
		int num = (this.state ? 2 : 0);
		this.mat.SetColor("_EmissionColor", new Color(num, num, num));
		base.Invoke("Blink", this.state ? 0.2f : 1.3f);
	}
}

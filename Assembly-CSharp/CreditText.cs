using UnityEngine;

public class CreditText : MonoBehaviour
{
	public bool move;

	public float speed;

	private void FixedUpdate()
	{
		if (this.move)
		{
			base.transform.Translate(Vector3.up * this.speed);
		}
	}
}

using UnityEngine;

public class BrowserLerp : MonoBehaviour
{
	private Vector3 prevPos;

	private RectTransform rectTransform;

	private Vector3 targetPos;

	public float speed = 2f;

	private void Start()
	{
		this.rectTransform = base.GetComponent<RectTransform>();
	}

	private void LateUpdate()
	{
		this.targetPos += this.rectTransform.localPosition - this.prevPos;
		this.rectTransform.localPosition = this.prevPos;
		this.rectTransform.localPosition = Vector3.Lerp(this.rectTransform.localPosition, this.targetPos, Time.deltaTime * this.speed * 4f);
		this.prevPos = this.rectTransform.localPosition;
	}
}

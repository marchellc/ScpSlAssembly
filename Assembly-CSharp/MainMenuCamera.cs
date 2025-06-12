using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
	public float borderWidthPercent;

	private float rotSpeed;

	private void Update()
	{
		float num = (float)Screen.width * (this.borderWidthPercent / 100f);
		Vector3 zero = Vector3.zero;
		Vector3 mousePosition = Input.mousePosition;
		if (mousePosition.x < num && base.transform.localRotation.eulerAngles.y > 41f)
		{
			zero += Vector3.down;
		}
		if (mousePosition.x > (float)Screen.width - num && base.transform.localRotation.eulerAngles.y < 74f)
		{
			zero += Vector3.up;
		}
		if (zero == Vector3.zero)
		{
			this.rotSpeed = 0f;
		}
		else
		{
			this.rotSpeed += Time.deltaTime * 200f;
			this.rotSpeed = Mathf.Clamp(this.rotSpeed, 0f, 120f);
		}
		zero.Normalize();
		base.transform.localRotation = Quaternion.Euler(base.transform.localRotation.eulerAngles + Time.deltaTime * this.rotSpeed * zero);
		if (Input.GetKeyDown(KeyCode.Mouse0))
		{
			this.Raycast();
		}
	}

	private void Raycast()
	{
		if (Physics.Raycast(base.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out var hitInfo))
		{
			this.ElementChoosen(hitInfo.transform.name);
		}
	}

	public void ElementChoosen(string id)
	{
		if (!(id == "EXIT"))
		{
			if (id == "PLAY")
			{
				Object.FindObjectOfType<NetManagerValueSetter>().HostGame();
			}
		}
		else
		{
			Debug.Log("Application closed by the user.");
			Shutdown.Quit();
		}
	}
}

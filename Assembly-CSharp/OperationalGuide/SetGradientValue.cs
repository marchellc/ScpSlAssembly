using UnityEngine;
using UnityEngine.UI;

namespace OperationalGuide;

public class SetGradientValue : MonoBehaviour
{
	private readonly int _leftColor = Shader.PropertyToID("_LeftColor");

	private readonly int _rightColor = Shader.PropertyToID("_RightColor");

	private readonly int _rotation = Shader.PropertyToID("_Rotation");

	private readonly int _leftColorModifier = Shader.PropertyToID("_LeftColorModifier");

	private readonly int _rightColorModifier = Shader.PropertyToID("_RightColorModifier");

	private readonly int _leftAlphaModifier = Shader.PropertyToID("_LeftAlphaModifier");

	private readonly int _rightAlphaModifier = Shader.PropertyToID("_RightAlphaModifier");

	public Color LeftColor;

	public Color RightColor;

	public float Rotation;

	public float LeftColorModifier;

	public float RightColorModifier;

	public float LeftAlphaModifier;

	public float RightAlphaModifier;

	private void Start()
	{
		Image component = base.GetComponent<Image>();
		component.material = new Material(component.material);
		component.material.SetColor(this._leftColor, this.LeftColor);
		component.material.SetColor(this._rightColor, this.RightColor);
		component.material.SetFloat(this._rotation, this.Rotation);
		component.material.SetFloat(this._leftColorModifier, this.LeftColorModifier);
		component.material.SetFloat(this._rightColorModifier, this.RightColorModifier);
		component.material.SetFloat(this._rightColorModifier, this.RightColorModifier);
		component.material.SetFloat(this._leftAlphaModifier, this.LeftAlphaModifier);
		component.material.SetFloat(this._rightAlphaModifier, this.RightAlphaModifier);
	}
}

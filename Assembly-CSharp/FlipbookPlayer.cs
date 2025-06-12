using UnityEngine;
using UnityEngine.UI;

public class FlipbookPlayer : MonoBehaviour
{
	[SerializeField]
	private int _updateCooldown;

	[SerializeField]
	private RawImage _image;

	[SerializeField]
	private Vector2 _primaryOffset;

	[SerializeField]
	private int _primaryCycles;

	[SerializeField]
	private Vector2 _secondaryOffset;

	[SerializeField]
	private int _secondaryCycles;

	private int _elapsedPrimary;

	private int _elapsedSecondary;

	private int _remainingCooldown;

	private Rect _rect;

	private void Awake()
	{
		this._rect = this._image.uvRect;
	}

	private void FixedUpdate()
	{
		if (this._remainingCooldown > 0)
		{
			this._remainingCooldown--;
			return;
		}
		this._rect.position = this._elapsedPrimary * this._primaryOffset + this._elapsedSecondary * this._secondaryOffset;
		this._elapsedPrimary++;
		if (this._elapsedPrimary >= this._primaryCycles)
		{
			this._elapsedSecondary++;
			this._elapsedPrimary = 0;
			if (this._elapsedSecondary >= this._secondaryCycles)
			{
				this._elapsedSecondary = 0;
			}
		}
		this._remainingCooldown = this._updateCooldown;
		this._image.uvRect = this._rect;
	}
}

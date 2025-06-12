using UnityEngine;

namespace Targeting;

public class Scp096Target : TargetComponent
{
	[SerializeField]
	private GameObject _targetParticles;

	private bool _isTarget;

	public override bool IsTarget
	{
		get
		{
			return this._isTarget;
		}
		set
		{
			this._targetParticles.SetActive(value);
			this._isTarget = value;
		}
	}

	private void Start()
	{
		this.IsTarget = false;
	}
}

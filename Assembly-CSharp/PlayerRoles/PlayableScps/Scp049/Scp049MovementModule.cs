using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp049;

public class Scp049MovementModule : FirstPersonMovementModule
{
	[SerializeField]
	private Scp049Role _role;

	private float _normalSpeed;

	private float _enragedSpeed;

	private Scp049SenseAbility _senseAbility;

	private float MovementSpeed
	{
		set
		{
			base.WalkSpeed = value;
			base.SprintSpeed = value;
		}
	}

	private void Awake()
	{
		this._normalSpeed = base.WalkSpeed;
		this._enragedSpeed = base.SprintSpeed;
		this._role.SubroutineModule.TryGetSubroutine<Scp049SenseAbility>(out this._senseAbility);
	}

	protected override void UpdateMovement()
	{
		this.MovementSpeed = (this._senseAbility.HasTarget ? this._enragedSpeed : this._normalSpeed);
		base.UpdateMovement();
	}
}

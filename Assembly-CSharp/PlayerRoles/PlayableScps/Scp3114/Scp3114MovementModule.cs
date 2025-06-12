using PlayerRoles.FirstPersonControl;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114MovementModule : FirstPersonMovementModule, IStaminaModifier
{
	private Scp3114Role _scpRole;

	private float _skeletonWalkSpeed;

	private float _skeletonSprintSpeed;

	protected override FpcStateProcessor NewStateProcessor => new SubroutineInfluencedFpcStateProcessor(base.Hub, this, FpcStateProcessor.DefaultUseRate, FpcStateProcessor.DefaultSpawnImmunity, FpcStateProcessor.DefaultRegenCooldown, FpcStateProcessor.DefaultRegenSpeed, 3.11f);

	public bool StaminaModifierActive => this._scpRole.SkeletonIdle;

	public float StaminaUsageMultiplier => 4f;

	private void Awake()
	{
		this._scpRole = base.GetComponent<Scp3114Role>();
		this._skeletonWalkSpeed = base.WalkSpeed;
		this._skeletonSprintSpeed = base.SprintSpeed;
		this._scpRole.CurIdentity.OnStatusChanged += OnStatusChanged;
	}

	private void OnStatusChanged()
	{
		switch (this._scpRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
		{
			if (PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this._scpRole.CurIdentity.StolenRole, out var result))
			{
				base.WalkSpeed = result.FpcModule.WalkSpeed;
				base.SprintSpeed = result.FpcModule.SprintSpeed;
			}
			break;
		}
		case Scp3114Identity.DisguiseStatus.None:
			base.WalkSpeed = this._skeletonWalkSpeed;
			base.SprintSpeed = this._skeletonSprintSpeed;
			break;
		}
	}
}

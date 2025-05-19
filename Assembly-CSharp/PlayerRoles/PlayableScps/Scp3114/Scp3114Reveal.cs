using System;
using Mirror;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Reveal : KeySubroutine<Scp3114Role>
{
	private const float HoldDuration = 0.65f;

	public const ActionName RevealKey = ActionName.Reload;

	private float _holdTimer;

	protected override ActionName TargetKey => ActionName.Reload;

	protected override bool KeyPressable
	{
		get
		{
			if (base.KeyPressable && base.CastRole.Disguised)
			{
				return _holdTimer < 0.65f;
			}
			return false;
		}
	}

	public static event Action OnRevealFail;

	protected override void Update()
	{
		base.Update();
		if (base.Role.IsLocalPlayer)
		{
			if (IsKeyHeld && base.CastRole.Disguised)
			{
				_holdTimer += Time.deltaTime;
			}
			else
			{
				_holdTimer = 0f;
			}
		}
	}

	protected override void OnKeyUp()
	{
		base.OnKeyUp();
		if (base.CastRole.Disguised)
		{
			if (_holdTimer >= 0.65f)
			{
				ClientSendCmd();
			}
			else
			{
				Scp3114Reveal.OnRevealFail?.Invoke();
			}
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		base.CastRole.Disguised = false;
	}
}

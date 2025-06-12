using System;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class RippleTriggerBase : StandardSubroutine<Scp939Role>
{
	private bool _playerSet;

	private RipplePlayer _player;

	private static int _playerIndex;

	protected RipplePlayer Player
	{
		get
		{
			if (this._playerSet)
			{
				return this._player;
			}
			this._player = base.CastRole.SubroutineModule.AllSubroutines[this.PlayerIndex] as RipplePlayer;
			this._playerSet = true;
			return this._player;
		}
	}

	protected bool IsLocalOrSpectated
	{
		get
		{
			if (!base.Owner.isLocalPlayer)
			{
				return base.Owner.IsLocallySpectated();
			}
			return true;
		}
	}

	private int PlayerIndex
	{
		get
		{
			if (RippleTriggerBase._playerIndex > 0)
			{
				return RippleTriggerBase._playerIndex;
			}
			SubroutineManagerModule subroutineModule = base.CastRole.SubroutineModule;
			for (int i = 0; i < subroutineModule.AllSubroutines.Length; i++)
			{
				if (subroutineModule.AllSubroutines[i] is RipplePlayer)
				{
					return RippleTriggerBase._playerIndex = i;
				}
			}
			throw new InvalidOperationException("SCP-939 has no RipplePlayer subroutine!");
		}
	}

	public static event Action<ReferenceHub> OnPlayedRippleLocally;

	protected void PlayInRange(Vector3 pos, float maxRange, Color color)
	{
		this.PlayInRangeSqr(pos, maxRange * maxRange, color);
	}

	protected void PlayInRangeSqr(Vector3 pos, float maxRangeSqr, Color color)
	{
		if (!((pos - base.CastRole.FpcModule.Position).sqrMagnitude > maxRangeSqr))
		{
			this.Player.Play(pos, color);
		}
	}

	protected void OnPlayedRipple(ReferenceHub hub)
	{
		RippleTriggerBase.OnPlayedRippleLocally?.Invoke(hub);
	}

	protected void ServerSendRpcToObservers()
	{
		base.ServerSendRpc((ReferenceHub x) => x == base.Owner || base.Owner.IsSpectatedBy(x));
	}

	protected bool CheckVisibility(ReferenceHub ply)
	{
		return base.CastRole.VisibilityController.ValidateVisibility(ply);
	}
}

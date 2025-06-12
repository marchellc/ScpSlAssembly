using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.HUDs;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp049;

public class Scp049CallAbility : KeySubroutine<Scp049Role>
{
	private const float BaseCooldown = 45f;

	private const float EffectDuration = 20f;

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public readonly AbilityCooldown Duration = new AbilityCooldown();

	private bool _serverTriggered;

	public AbilityHud CallAbilityHUD;

	public bool IsMarkerShown
	{
		get
		{
			if (!this.Duration.IsReady)
			{
				if (NetworkServer.active)
				{
					return this._serverTriggered;
				}
				return true;
			}
			return false;
		}
	}

	protected override ActionName TargetKey => ActionName.Reload;

	private void ServerRefreshDuration()
	{
		if (this._serverTriggered && this.Duration.IsReady)
		{
			this.Cooldown.Trigger(45.0);
			this._serverTriggered = false;
			base.ServerSendRpc(toAll: true);
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		if (!this._serverTriggered && this.Cooldown.IsReady)
		{
			Scp049UsingDoctorsCallEventArgs e = new Scp049UsingDoctorsCallEventArgs(base.Owner);
			Scp049Events.OnUsingDoctorsCall(e);
			if (e.IsAllowed)
			{
				this.Duration.Trigger(20.0);
				this._serverTriggered = true;
				base.ServerSendRpc(toAll: true);
				Scp049Events.OnUsedDoctorsCall(new Scp049UsedDoctorsCallEventArgs(base.Owner));
			}
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		this.Cooldown.WriteCooldown(writer);
		this.Duration.WriteCooldown(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		this.Cooldown.ReadCooldown(reader);
		this.Duration.ReadCooldown(reader);
		if (this.Cooldown.Remaining >= 45f)
		{
			this.AbilityAudio(start: false);
		}
		else if (this.Duration.Remaining >= 20f)
		{
			this.AbilityAudio(start: true);
		}
	}

	private void AbilityAudio(bool start)
	{
	}

	protected override void Update()
	{
		base.Update();
		if (NetworkServer.active)
		{
			this.ServerRefreshDuration();
		}
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (this.Cooldown.IsReady && this.Duration.IsReady)
		{
			base.ClientSendCmd();
		}
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
		this.Duration.Clear();
		this._serverTriggered = false;
	}
}

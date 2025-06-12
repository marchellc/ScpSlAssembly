using System;
using System.Text;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.Ragdolls;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Identity : StandardSubroutine<Scp3114Role>, ICustomNicknameDisplayRole
{
	public enum DisguiseStatus
	{
		None,
		Equipping,
		Active
	}

	public class StolenIdentity
	{
		private DisguiseStatus _status;

		public BasicRagdoll Ragdoll;

		public byte UnitNameId;

		public DisguiseStatus Status
		{
			get
			{
				return this._status;
			}
			set
			{
				if (this._status != value)
				{
					this._status = value;
					this.OnStatusChanged?.Invoke();
				}
			}
		}

		public RoleTypeId StolenRole
		{
			get
			{
				if (!(this.Ragdoll == null))
				{
					return this.Ragdoll.Info.RoleType;
				}
				return RoleTypeId.None;
			}
		}

		public event Action OnStatusChanged;

		public void Reset()
		{
			this._status = DisguiseStatus.None;
			this.Ragdoll = null;
			this.UnitNameId = 0;
		}
	}

	public readonly StolenIdentity CurIdentity = new StolenIdentity();

	public readonly AbilityCooldown RemainingDuration = new AbilityCooldown();

	private bool _wasDisguised;

	[SerializeField]
	private AudioSource[] _revealEffectSources;

	[SerializeField]
	private AudioSource _revealWarningSource;

	[SerializeField]
	private float _warningTimeSeconds;

	[SerializeField]
	private float _disguiseDurationSeconds;

	public Color NicknameColor
	{
		get
		{
			if (this.CurIdentity.Status != DisguiseStatus.Active)
			{
				return base.Role.RoleColor;
			}
			if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(this.CurIdentity.StolenRole, out var result))
			{
				return base.Role.RoleColor;
			}
			return result.RoleColor;
		}
	}

	private void OnRagdollRemoved(BasicRagdoll ragdoll)
	{
		if (!(this.CurIdentity.Ragdoll != ragdoll))
		{
			this.CurIdentity.Status = DisguiseStatus.None;
		}
	}

	private void OnPlayerAdded(ReferenceHub player)
	{
		if (NetworkServer.active)
		{
			base.ServerSendRpc(player);
		}
	}

	private void Update()
	{
		this.UpdateWarningAudio();
		if (NetworkServer.active && this.CurIdentity.Status == DisguiseStatus.Active && this.RemainingDuration.IsReady)
		{
			this.CurIdentity.Status = DisguiseStatus.None;
			this.ServerResendIdentity();
		}
	}

	private void UpdateWarningAudio()
	{
		double num = this.RemainingDuration.Remaining;
		bool flag = num < (double)this._warningTimeSeconds && num > 0.0;
		if (this._revealWarningSource.isPlaying)
		{
			if (!flag)
			{
				this._revealWarningSource.Stop();
			}
		}
		else
		{
			if (!flag)
			{
				return;
			}
			this._revealWarningSource.Play();
		}
		this._revealWarningSource.mute = !base.Role.IsLocalPlayer && !base.Owner.IsLocallySpectated();
	}

	private void OnIdentityStatusChanged()
	{
		if (this.CurIdentity.Status == DisguiseStatus.Active)
		{
			this._wasDisguised = true;
			this.RemainingDuration.Trigger(this._disguiseDurationSeconds);
			return;
		}
		this.RemainingDuration.Clear();
		if (this._wasDisguised)
		{
			this._wasDisguised = false;
			this._revealEffectSources.ForEach(delegate(AudioSource x)
			{
				x.Play();
			});
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this.CurIdentity.OnStatusChanged += OnIdentityStatusChanged;
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		ReferenceHub.OnPlayerAdded += OnPlayerAdded;
		RagdollManager.OnRagdollRemoved += OnRagdollRemoved;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._wasDisguised = false;
		this.RemainingDuration.Clear();
		this.CurIdentity.Status = DisguiseStatus.None;
		ReferenceHub.OnPlayerAdded -= OnPlayerAdded;
		RagdollManager.OnRagdollRemoved -= OnRagdollRemoved;
	}

	public void WriteNickname(StringBuilder sb)
	{
	}

	public void ServerResendIdentity()
	{
		base.ServerSendRpc(toAll: true);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		this.RemainingDuration.WriteCooldown(writer);
		writer.WriteNetworkBehaviour(this.CurIdentity.Ragdoll);
		writer.WriteByte(this.CurIdentity.UnitNameId);
		writer.WriteByte((byte)this.CurIdentity.Status);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		if (!NetworkServer.active)
		{
			this.RemainingDuration.ReadCooldown(reader);
			this.CurIdentity.Ragdoll = reader.ReadNetworkBehaviour<BasicRagdoll>();
			this.CurIdentity.UnitNameId = reader.ReadByte();
			this.CurIdentity.Status = (DisguiseStatus)reader.ReadByte();
		}
	}
}

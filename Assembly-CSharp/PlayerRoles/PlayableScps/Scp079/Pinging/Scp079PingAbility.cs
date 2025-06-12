using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Spectating;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Pinging;

public class Scp079PingAbility : Scp079KeyAbilityBase
{
	[SerializeField]
	private int _cost;

	[SerializeField]
	private float _instantCooldown;

	[SerializeField]
	private float _groupCooldown;

	[SerializeField]
	private int _groupSize;

	[SerializeField]
	private Scp079PingInstance _prefab;

	[SerializeField]
	private Sprite[] _icons;

	private string _abilityName;

	private string _cooldownMsg;

	private RateLimiter _rateLimiter;

	private byte _syncProcessorIndex;

	private RelativePosition _syncPos;

	private Vector3 _syncNormal;

	private const float RaycastMaxDis = 130f;

	private static readonly IPingProcessor[] PingProcessors = new IPingProcessor[7]
	{
		new GeneratorPingProcessor(),
		new ProjectilePingProcessor(),
		new MicroHidPingProcesssor(),
		new HumanPingProcessor(),
		new ElevatorPingProcessor(),
		new DoorPingProcessor(),
		new DefaultPingProcessor()
	};

	public override ActionName ActivationKey => ActionName.Scp079PingLocation;

	public override bool IsReady
	{
		get
		{
			if (base.AuxManager.CurrentAux >= (float)this._cost)
			{
				return this._rateLimiter.AllReady;
			}
			return false;
		}
	}

	public override bool IsVisible => !Scp079CursorManager.LockCameras;

	public override string AbilityName => string.Format(this._abilityName, this._cost);

	public override string FailMessage
	{
		get
		{
			if (!(base.AuxManager.CurrentAux < (float)this._cost))
			{
				if (!this._rateLimiter.RateReady)
				{
					return this._cooldownMsg;
				}
				return null;
			}
			return base.GetNoAuxMessage(this._cost);
		}
	}

	private void SpawnIndicator(int processorIndex, RelativePosition pos, Vector3 normal)
	{
	}

	private void WriteSyncVars(NetworkWriter writer)
	{
		writer.WriteByte(this._syncProcessorIndex);
		writer.WriteRelativePosition(this._syncPos);
		writer.WriteVector3(this._syncNormal);
	}

	private bool ServerCheckReceiver(ReferenceHub hub, Vector3 point, int processorIndex)
	{
		PlayerRoleBase currentRole = hub.roleManager.CurrentRole;
		if (!(currentRole is FpcStandardScp fpcStandardScp))
		{
			if (!hub.IsSCP())
			{
				return currentRole is SpectatorRole;
			}
			return true;
		}
		float range = Scp079PingAbility.PingProcessors[processorIndex].Range;
		float num = range * range;
		return (fpcStandardScp.FpcModule.Position - point).sqrMagnitude < num;
	}

	protected override void Start()
	{
		base.Start();
		this._rateLimiter = new RateLimiter(this._instantCooldown, this._groupSize, this._groupCooldown);
		this._abilityName = Translations.Get(Scp079HudTranslation.PingLocation);
		this._cooldownMsg = Translations.Get(Scp079HudTranslation.PingRateLimited);
	}

	protected override void Trigger()
	{
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		this.WriteSyncVars(writer);
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (!this.IsReady || !base.Role.TryGetOwner(out var _) || base.LostSignalHandler.Lost)
		{
			return;
		}
		this._syncProcessorIndex = reader.ReadByte();
		if (this._syncProcessorIndex >= Scp079PingAbility.PingProcessors.Length)
		{
			return;
		}
		this._syncPos = reader.ReadRelativePosition();
		this._syncNormal = reader.ReadVector3();
		Scp079PingingEventArgs e = new Scp079PingingEventArgs(base.Owner, this._syncPos.Position, this._syncNormal, this._syncProcessorIndex);
		Scp079Events.OnPinging(e);
		if (e.IsAllowed)
		{
			this._syncPos = new RelativePosition(e.Position);
			this._syncNormal = e.Normal;
			this._syncProcessorIndex = (byte)e.PingType;
			base.ServerSendRpc((ReferenceHub x) => this.ServerCheckReceiver(x, this._syncPos.Position, this._syncProcessorIndex));
			base.AuxManager.CurrentAux -= this._cost;
			this._rateLimiter.RegisterInput();
			Scp079Events.OnPinged(new Scp079PingedEventArgs(base.Owner, this._syncPos.Position, this._syncNormal, this._syncProcessorIndex));
		}
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		this.WriteSyncVars(writer);
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.SpawnIndicator(reader.ReadByte(), reader.ReadRelativePosition(), reader.ReadVector3());
	}
}

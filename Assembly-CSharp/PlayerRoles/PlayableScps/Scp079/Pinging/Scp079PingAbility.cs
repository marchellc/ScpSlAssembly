using System;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.Spectating;
using RelativePositioning;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Pinging
{
	public class Scp079PingAbility : Scp079KeyAbilityBase
	{
		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Scp079PingLocation;
			}
		}

		public override bool IsReady
		{
			get
			{
				return base.AuxManager.CurrentAux >= (float)this._cost && this._rateLimiter.AllReady;
			}
		}

		public override bool IsVisible
		{
			get
			{
				return !Scp079CursorManager.LockCameras;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._abilityName, this._cost);
			}
		}

		public override string FailMessage
		{
			get
			{
				if (base.AuxManager.CurrentAux < (float)this._cost)
				{
					return base.GetNoAuxMessage((float)this._cost);
				}
				if (!this._rateLimiter.RateReady)
				{
					return this._cooldownMsg;
				}
				return null;
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
			FpcStandardScp fpcStandardScp = currentRole as FpcStandardScp;
			if (fpcStandardScp == null)
			{
				return hub.IsSCP(true) || currentRole is SpectatorRole;
			}
			float range = Scp079PingAbility.PingProcessors[processorIndex].Range;
			float num = range * range;
			Vector3 position = fpcStandardScp.FpcModule.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(point, true);
			if (roomIdentifier == null)
			{
				return false;
			}
			Vector3 gridScale = RoomIdentifier.GridScale;
			Vector3Int[] occupiedCoords = roomIdentifier.OccupiedCoords;
			for (int i = 0; i < occupiedCoords.Length; i++)
			{
				if (new Bounds(Vector3.Scale(occupiedCoords[i], gridScale), gridScale).SqrDistance(position) <= num)
				{
					return true;
				}
			}
			return false;
		}

		protected override void Start()
		{
			base.Start();
			this._rateLimiter = new RateLimiter(this._instantCooldown, this._groupSize, this._groupCooldown);
			this._abilityName = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.PingLocation);
			this._cooldownMsg = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.PingRateLimited);
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
			ReferenceHub referenceHub;
			if (!this.IsReady || !base.Role.TryGetOwner(out referenceHub) || base.LostSignalHandler.Lost)
			{
				return;
			}
			this._syncProcessorIndex = reader.ReadByte();
			if ((int)this._syncProcessorIndex >= Scp079PingAbility.PingProcessors.Length)
			{
				return;
			}
			this._syncPos = reader.ReadRelativePosition();
			this._syncNormal = reader.ReadVector3();
			base.ServerSendRpc((ReferenceHub x) => this.ServerCheckReceiver(x, this._syncPos.Position, (int)this._syncProcessorIndex));
			base.AuxManager.CurrentAux -= (float)this._cost;
			this._rateLimiter.RegisterInput();
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			this.WriteSyncVars(writer);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.SpawnIndicator((int)reader.ReadByte(), reader.ReadRelativePosition(), reader.ReadVector3());
		}

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

		private static readonly IPingProcessor[] PingProcessors = new IPingProcessor[]
		{
			new GeneratorPingProcessor(),
			new ProjectilePingProcessor(),
			new MicroHidPingProcesssor(),
			new HumanPingProcessor(),
			new ElevatorPingProcessor(),
			new DoorPingProcessor(),
			new DefaultPingProcessor()
		};
	}
}

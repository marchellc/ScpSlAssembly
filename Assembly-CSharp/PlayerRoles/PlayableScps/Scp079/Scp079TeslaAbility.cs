using System;
using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079TeslaAbility : Scp079KeyAbilityBase
	{
		public override bool IsVisible
		{
			get
			{
				if (!Scp079CursorManager.LockCameras)
				{
					TeslaOvercon teslaOvercon = OverconManager.Singleton.HighlightedOvercon as TeslaOvercon;
					if (teslaOvercon != null)
					{
						return teslaOvercon != null;
					}
				}
				return false;
			}
		}

		public override bool IsReady
		{
			get
			{
				return base.AuxManager.CurrentAux >= (float)this._cost && this._nextUseTime < NetworkTime.time;
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
				int num = Mathf.CeilToInt((float)(this._nextUseTime - NetworkTime.time));
				if (num > 0)
				{
					return this._cooldownMessage + "\n" + base.AuxManager.GenerateCustomETA(num);
				}
				return null;
			}
		}

		public override ActionName ActivationKey
		{
			get
			{
				return ActionName.Shoot;
			}
		}

		public override string AbilityName
		{
			get
			{
				return string.Format(this._abilityName, this._cost);
			}
		}

		protected override void Start()
		{
			base.Start();
			this._abilityName = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.FireTeslaGate);
			this._cooldownMessage = Translations.Get<Scp079HudTranslation>(Scp079HudTranslation.TeslaGateCooldown);
		}

		protected override void Trigger()
		{
			base.ClientSendCmd();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this._nextUseTime = 0.0;
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!this.IsReady)
			{
				return;
			}
			Scp079Camera cam = base.CurrentCamSync.CurrentCamera;
			TeslaGate teslaGate;
			if (!TeslaGate.AllGates.TryGetFirst((TeslaGate x) => RoomUtils.IsTheSameRoom(cam.Position, x.transform.position), out teslaGate))
			{
				return;
			}
			Scp079UsingTeslaEventArgs scp079UsingTeslaEventArgs = new Scp079UsingTeslaEventArgs(base.Owner, teslaGate);
			Scp079Events.OnUsingTesla(scp079UsingTeslaEventArgs);
			if (!scp079UsingTeslaEventArgs.IsAllowed)
			{
				return;
			}
			base.RewardManager.MarkRoom(cam.Room);
			base.AuxManager.CurrentAux -= (float)this._cost;
			teslaGate.RpcInstantBurst();
			this._nextUseTime = NetworkTime.time + (double)this._cooldown;
			base.ServerSendRpc(false);
			Scp079Events.OnUsedTesla(new Scp079UsedTeslaEventArgs(base.Owner, teslaGate));
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteDouble(this._nextUseTime);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this._nextUseTime = reader.ReadDouble();
		}

		[SerializeField]
		private int _cost;

		[SerializeField]
		private float _cooldown;

		private string _abilityName;

		private string _cooldownMessage;

		private double _nextUseTime;
	}
}

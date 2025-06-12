using LabApi.Events.Arguments.Scp079Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.GUI;
using PlayerRoles.PlayableScps.Scp079.Overcons;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079TeslaAbility : Scp079KeyAbilityBase
{
	[SerializeField]
	private int _cost;

	[SerializeField]
	private float _cooldown;

	private string _abilityName;

	private string _cooldownMessage;

	private double _nextUseTime;

	public override bool IsVisible
	{
		get
		{
			if (!Scp079CursorManager.LockCameras && OverconManager.Singleton.HighlightedOvercon is TeslaOvercon teslaOvercon)
			{
				return teslaOvercon != null;
			}
			return false;
		}
	}

	public override bool IsReady
	{
		get
		{
			if (base.AuxManager.CurrentAux >= (float)this._cost)
			{
				return this._nextUseTime < NetworkTime.time;
			}
			return false;
		}
	}

	public override string FailMessage
	{
		get
		{
			if (base.AuxManager.CurrentAux < (float)this._cost)
			{
				return base.GetNoAuxMessage(this._cost);
			}
			int num = Mathf.CeilToInt((float)(this._nextUseTime - NetworkTime.time));
			if (num > 0)
			{
				return this._cooldownMessage + "\n" + base.AuxManager.GenerateCustomETA(num);
			}
			return null;
		}
	}

	public override ActionName ActivationKey => ActionName.Shoot;

	public override string AbilityName => string.Format(this._abilityName, this._cost);

	public override bool DummyEmulationSupport => true;

	protected override void Start()
	{
		base.Start();
		this._abilityName = Translations.Get(Scp079HudTranslation.FireTeslaGate);
		this._cooldownMessage = Translations.Get(Scp079HudTranslation.TeslaGateCooldown);
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
		if (TeslaGate.AllGates.TryGetFirst((TeslaGate x) => cam.Position.CompareCoords(x.transform.position), out var first))
		{
			Scp079UsingTeslaEventArgs e = new Scp079UsingTeslaEventArgs(base.Owner, first);
			Scp079Events.OnUsingTesla(e);
			if (e.IsAllowed)
			{
				base.RewardManager.MarkRoom(cam.Room);
				base.AuxManager.CurrentAux -= this._cost;
				first.RpcInstantBurst();
				this._nextUseTime = NetworkTime.time + (double)this._cooldown;
				base.ServerSendRpc(toAll: false);
				Scp079Events.OnUsedTesla(new Scp079UsedTeslaEventArgs(base.Owner, first));
			}
		}
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
}

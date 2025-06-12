using System;
using System.Collections.Generic;
using System.Diagnostics;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.Scp939Events;
using LabApi.Events.Handlers;
using MapGeneration;
using Mirror;
using PlayerRoles.Spectating;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp939;

public class Scp939AmnesticCloudAbility : KeySubroutine<Scp939Role>
{
	private static readonly Dictionary<RoomName, float[]> WhitelistedFloors = new Dictionary<RoomName, float[]>
	{
		[RoomName.EzOfficeSmall] = new float[1] { -0.527f },
		[RoomName.EzOfficeStoried] = new float[1] { 3.767f },
		[RoomName.Hcz106] = new float[1] { 1.19f },
		[RoomName.Hcz079] = new float[1] { -4.334f },
		[RoomName.HczWarhead] = new float[3] { -75.33f, -72.33f, -70.26f }
	};

	private const float FloorTolerance = 0.2f;

	private bool _targetState;

	private float _sendDuration;

	private Scp939FocusAbility _focusAbility;

	private readonly Stopwatch _beginHeldSw = Stopwatch.StartNew();

	[SerializeField]
	private Scp939AmnesticCloudInstance _instancePrefab;

	[SerializeField]
	private float _failedCooldown;

	[SerializeField]
	private float _placedCooldown;

	public readonly AbilityCooldown Duration = new AbilityCooldown();

	public readonly AbilityCooldown Cooldown = new AbilityCooldown();

	public readonly AbilityCooldown HudIndicatorMin = new AbilityCooldown();

	public readonly AbilityCooldown HudIndicatorMax = new AbilityCooldown();

	protected override ActionName TargetKey => ActionName.ToggleFlashlight;

	protected override bool KeyPressable
	{
		get
		{
			if (base.KeyPressable)
			{
				return this._focusAbility.State == 0f;
			}
			return false;
		}
	}

	public float HoldDuration => (float)this._beginHeldSw.Elapsed.TotalSeconds;

	public bool TargetState
	{
		get
		{
			return this._targetState;
		}
		set
		{
			if (this._targetState != value)
			{
				this._targetState = value;
				if (value)
				{
					this.OnStateEnabled();
				}
				else
				{
					this.OnStateDisabled();
				}
			}
		}
	}

	public event Action<Scp939HudTranslation> OnDeployFailed;

	private void OnStateEnabled()
	{
		Scp939CreatingAmnesticCloudEventArgs e = new Scp939CreatingAmnesticCloudEventArgs(base.Owner);
		Scp939Events.OnCreatingAmnesticCloud(e);
		if (e.IsAllowed)
		{
			this._beginHeldSw.Restart();
			this.HudIndicatorMax.Clear();
			this.HudIndicatorMin.Trigger(this._instancePrefab.MinMaxTime.y);
			if (NetworkServer.active)
			{
				Scp939AmnesticCloudInstance scp939AmnesticCloudInstance = UnityEngine.Object.Instantiate(this._instancePrefab);
				scp939AmnesticCloudInstance.ServerSetup(base.Owner);
				Scp939Events.OnCreatedAmnesticCloud(new Scp939CreatedAmnesticCloudEventArgs(base.Owner, scp939AmnesticCloudInstance));
			}
		}
	}

	private void OnStateDisabled()
	{
		this._beginHeldSw.Reset();
	}

	protected override void Awake()
	{
		base.Awake();
		base.GetSubroutine<Scp939FocusAbility>(out this._focusAbility);
	}

	protected override void Update()
	{
		base.Update();
		if ((base.Owner.isLocalPlayer || base.Owner.IsLocallySpectated()) && this.TargetState && this.HudIndicatorMax.IsReady)
		{
			Vector2 minMaxTime = this._instancePrefab.MinMaxTime;
			if (!(this.HoldDuration < minMaxTime.x))
			{
				this.HudIndicatorMax.NextUse = this.HudIndicatorMin.NextUse;
				this.HudIndicatorMax.InitialTime = this.HudIndicatorMin.InitialTime;
			}
		}
	}

	protected override void OnKeyDown()
	{
		base.OnKeyDown();
		if (this.Cooldown.IsReady)
		{
			if (!this.ValidateFloor())
			{
				this.ClientCancel(Scp939HudTranslation.CloudFailedPositionInvalid);
				return;
			}
			this.TargetState = true;
			base.ClientSendCmd();
		}
	}

	protected override void OnKeyUp()
	{
		base.OnKeyUp();
		if (this.TargetState)
		{
			this.ClientCancel(Scp939HudTranslation.PressKeyToLunge);
		}
	}

	public bool ValidateFloor()
	{
		Vector3 pos = base.CastRole.FpcModule.Position;
		if (!pos.TryGetRoom(out var room))
		{
			return false;
		}
		if (Scp939AmnesticCloudAbility.WhitelistedFloors.TryGetValue(room.Name, out var value))
		{
			float num = room.transform.position.y - pos.y;
			float[] array = value;
			foreach (float num2 in array)
			{
				if (!(Mathf.Abs(num + num2) > 0.2f))
				{
					return true;
				}
			}
		}
		if (!DoorVariant.DoorsByRoom.TryGetValue(room, out var value2))
		{
			return false;
		}
		float halfHeight = base.CastRole.FpcModule.CharController.height / 2f;
		return value2.Any((DoorVariant x) => Mathf.Abs(x.transform.position.y - pos.y + halfHeight) < 0.2f);
	}

	public void ClientCancel(Scp939HudTranslation reason)
	{
		if ((uint)(reason - 12) <= 1u)
		{
			this.OnDeployFailed?.Invoke(reason);
		}
		this.TargetState = false;
		base.ClientSendCmd();
	}

	public void ServerConfirmPlacement(float duration)
	{
		this._sendDuration = duration;
		this.Cooldown.Trigger(this._placedCooldown);
		base.ServerSendRpc(toAll: true);
	}

	public void ServerFailPlacement()
	{
		this._sendDuration = -128f;
		this.Cooldown.Trigger(this._failedCooldown);
		base.ServerSendRpc(toAll: true);
	}

	public override void ClientWriteCmd(NetworkWriter writer)
	{
		base.ClientWriteCmd(writer);
		writer.WriteBool(this.TargetState);
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this.Cooldown.Clear();
		this.Duration.Clear();
		this.HudIndicatorMin.Clear();
		this.HudIndicatorMax.Clear();
		this.TargetState = false;
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		bool flag = reader.ReadBool();
		bool toAll = flag != this.TargetState;
		if (flag)
		{
			if (this.Cooldown.IsReady)
			{
				this.TargetState = flag;
				this.Cooldown.Trigger(this._failedCooldown);
			}
		}
		else
		{
			this.TargetState = false;
		}
		base.ServerSendRpc(toAll);
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteBool(this.TargetState);
		if (!this.TargetState)
		{
			this.Cooldown.WriteCooldown(writer);
			writer.WriteFloat(this._sendDuration);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		this.TargetState = reader.ReadBool();
		if (!this.TargetState)
		{
			this.Cooldown.ReadCooldown(reader);
			float num = reader.ReadFloat();
			if (num <= 0f)
			{
				this.Duration.Clear();
				return;
			}
			this.Duration.Trigger(num);
			this.Cooldown.InitialTime += num;
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		CustomNetworkManager.OnClientStarted += delegate
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<Scp939Role>(RoleTypeId.Scp939, out var result))
			{
				throw new InvalidOperationException("Cannot register amnestic cloud. SCP-939 role template not found.");
			}
			if (!result.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out var subroutine))
			{
				throw new InvalidOperationException("Cannot register amnestic cloud. Ability not found.");
			}
			NetworkClient.RegisterPrefab(subroutine._instancePrefab.gameObject);
		};
	}
}

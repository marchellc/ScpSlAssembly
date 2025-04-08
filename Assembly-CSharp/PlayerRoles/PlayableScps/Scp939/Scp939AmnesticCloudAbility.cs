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

namespace PlayerRoles.PlayableScps.Scp939
{
	public class Scp939AmnesticCloudAbility : KeySubroutine<Scp939Role>
	{
		public event Action<Scp939HudTranslation> OnDeployFailed;

		protected override ActionName TargetKey
		{
			get
			{
				return ActionName.ToggleFlashlight;
			}
		}

		protected override bool KeyPressable
		{
			get
			{
				return base.KeyPressable && this._focusAbility.State == 0f;
			}
		}

		public float HoldDuration
		{
			get
			{
				return (float)this._beginHeldSw.Elapsed.TotalSeconds;
			}
		}

		public bool TargetState
		{
			get
			{
				return this._targetState;
			}
			set
			{
				if (this._targetState == value)
				{
					return;
				}
				this._targetState = value;
				if (value)
				{
					this.OnStateEnabled();
					return;
				}
				this.OnStateDisabled();
			}
		}

		private void OnStateEnabled()
		{
			Scp939CreatingAmnesticCloudEventArgs scp939CreatingAmnesticCloudEventArgs = new Scp939CreatingAmnesticCloudEventArgs(base.Owner);
			Scp939Events.OnCreatingAmnesticCloud(scp939CreatingAmnesticCloudEventArgs);
			if (!scp939CreatingAmnesticCloudEventArgs.IsAllowed)
			{
				return;
			}
			this._beginHeldSw.Restart();
			this.HudIndicatorMax.Clear();
			this.HudIndicatorMin.Trigger((double)this._instancePrefab.MinMaxTime.y);
			if (!NetworkServer.active)
			{
				return;
			}
			Scp939AmnesticCloudInstance scp939AmnesticCloudInstance = global::UnityEngine.Object.Instantiate<Scp939AmnesticCloudInstance>(this._instancePrefab);
			scp939AmnesticCloudInstance.ServerSetup(base.Owner);
			Scp939Events.OnCreatedAmnesticCloud(new Scp939CreatedAmnesticCloudEventArgs(base.Owner, scp939AmnesticCloudInstance));
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
			if (!base.Owner.isLocalPlayer && !base.Owner.IsLocallySpectated())
			{
				return;
			}
			if (!this.TargetState || !this.HudIndicatorMax.IsReady)
			{
				return;
			}
			Vector2 minMaxTime = this._instancePrefab.MinMaxTime;
			if (this.HoldDuration < minMaxTime.x)
			{
				return;
			}
			this.HudIndicatorMax.NextUse = this.HudIndicatorMin.NextUse;
			this.HudIndicatorMax.InitialTime = this.HudIndicatorMin.InitialTime;
		}

		protected override void OnKeyDown()
		{
			base.OnKeyDown();
			if (!this.Cooldown.IsReady)
			{
				return;
			}
			if (!this.ValidateFloor())
			{
				this.ClientCancel(Scp939HudTranslation.CloudFailedPositionInvalid);
				return;
			}
			this.TargetState = true;
			base.ClientSendCmd();
		}

		protected override void OnKeyUp()
		{
			base.OnKeyUp();
			if (!this.TargetState)
			{
				return;
			}
			this.ClientCancel(Scp939HudTranslation.PressKeyToLunge);
		}

		public bool ValidateFloor()
		{
			Vector3 pos = base.CastRole.FpcModule.Position;
			RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(pos, true);
			if (roomIdentifier == null)
			{
				return false;
			}
			float[] array;
			if (Scp939AmnesticCloudAbility.WhitelistedFloors.TryGetValue(roomIdentifier.Name, out array))
			{
				float num = roomIdentifier.transform.position.y - pos.y;
				foreach (float num2 in array)
				{
					if (Mathf.Abs(num + num2) <= 0.2f)
					{
						return true;
					}
				}
			}
			HashSet<DoorVariant> hashSet;
			if (!DoorVariant.DoorsByRoom.TryGetValue(roomIdentifier, out hashSet))
			{
				return false;
			}
			float halfHeight = base.CastRole.FpcModule.CharController.height / 2f;
			return hashSet.Any((DoorVariant x) => Mathf.Abs(x.transform.position.y - pos.y + halfHeight) < 0.2f);
		}

		public void ClientCancel(Scp939HudTranslation reason)
		{
			if (reason - Scp939HudTranslation.CloudFailedPositionInvalid <= 1)
			{
				Action<Scp939HudTranslation> onDeployFailed = this.OnDeployFailed;
				if (onDeployFailed != null)
				{
					onDeployFailed(reason);
				}
			}
			this.TargetState = false;
			base.ClientSendCmd();
		}

		public void ServerConfirmPlacement(float duration)
		{
			this._sendDuration = duration;
			this.Cooldown.Trigger((double)this._placedCooldown);
			base.ServerSendRpc(true);
		}

		public void ServerFailPlacement()
		{
			this._sendDuration = -128f;
			this.Cooldown.Trigger((double)this._failedCooldown);
			base.ServerSendRpc(true);
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
			bool flag2 = flag != this.TargetState;
			if (flag)
			{
				if (this.Cooldown.IsReady)
				{
					this.TargetState = flag;
					this.Cooldown.Trigger((double)this._failedCooldown);
				}
			}
			else
			{
				this.TargetState = false;
			}
			base.ServerSendRpc(flag2);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBool(this.TargetState);
			if (this.TargetState)
			{
				return;
			}
			this.Cooldown.WriteCooldown(writer);
			writer.WriteFloat(this._sendDuration);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			this.TargetState = reader.ReadBool();
			if (this.TargetState)
			{
				return;
			}
			this.Cooldown.ReadCooldown(reader);
			float num = reader.ReadFloat();
			if (num <= 0f)
			{
				this.Duration.Clear();
				return;
			}
			this.Duration.Trigger((double)num);
			this.Cooldown.InitialTime += (double)num;
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientStarted += delegate
			{
				Scp939Role scp939Role;
				if (!PlayerRoleLoader.TryGetRoleTemplate<Scp939Role>(RoleTypeId.Scp939, out scp939Role))
				{
					throw new InvalidOperationException("Cannot register amnestic cloud. SCP-939 role template not found.");
				}
				Scp939AmnesticCloudAbility scp939AmnesticCloudAbility;
				if (!scp939Role.SubroutineModule.TryGetSubroutine<Scp939AmnesticCloudAbility>(out scp939AmnesticCloudAbility))
				{
					throw new InvalidOperationException("Cannot register amnestic cloud. Ability not found.");
				}
				NetworkClient.RegisterPrefab(scp939AmnesticCloudAbility._instancePrefab.gameObject);
			};
		}

		// Note: this type is marked as 'beforefieldinit'.
		static Scp939AmnesticCloudAbility()
		{
			Dictionary<RoomName, float[]> dictionary = new Dictionary<RoomName, float[]>();
			dictionary[RoomName.EzOfficeSmall] = new float[] { -0.527f };
			dictionary[RoomName.EzOfficeStoried] = new float[] { 3.767f };
			dictionary[RoomName.Hcz106] = new float[] { 1.19f };
			dictionary[RoomName.Hcz079] = new float[] { -4.334f };
			dictionary[RoomName.HczWarhead] = new float[] { -75.33f, -72.33f, -70.26f };
			Scp939AmnesticCloudAbility.WhitelistedFloors = dictionary;
		}

		private static readonly Dictionary<RoomName, float[]> WhitelistedFloors;

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
	}
}

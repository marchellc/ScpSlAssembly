using System;
using System.Runtime.InteropServices;
using GameCore;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;
using Utils.ConfigHandler;

namespace Scp914
{
	public class Scp914Controller : NetworkBehaviour, IServerInteractable, IInteractable
	{
		public static Scp914Controller Singleton { get; private set; }

		public static Vector3 MoveVector
		{
			get
			{
				return Scp914Controller.Singleton.OutputChamber.position - Scp914Controller.Singleton.IntakeChamber.position;
			}
		}

		public Transform IntakeChamber { get; private set; }

		public Transform OutputChamber { get; private set; }

		private Vector3 IntakeChamberSize
		{
			get
			{
				Vector3 vector = this.IntakeChamber.rotation * this.ChamberSize / 2f;
				return new Vector3(Mathf.Abs(vector.z), Mathf.Abs(vector.y), Mathf.Abs(vector.x));
			}
		}

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public Scp914KnobSetting KnobSetting
		{
			get
			{
				return this._knobSetting;
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (this.RemainingCooldown > 0f)
			{
				return;
			}
			if (colliderId != 0)
			{
				if (colliderId != 1)
				{
					return;
				}
				Scp914ActivatingEventArgs scp914ActivatingEventArgs = new Scp914ActivatingEventArgs(this._knobSetting, ply);
				Scp914Events.OnActivating(scp914ActivatingEventArgs);
				if (!scp914ActivatingEventArgs.IsAllowed)
				{
					return;
				}
				this.Network_knobSetting = scp914ActivatingEventArgs.KnobSetting;
				this.Upgrade();
				Scp914Events.OnActivated(new Scp914ActivatedEventArgs(this._knobSetting, ply));
				return;
			}
			else
			{
				Scp914KnobSetting scp914KnobSetting = this._knobSetting + 1;
				if (!Enum.IsDefined(typeof(Scp914KnobSetting), scp914KnobSetting))
				{
					scp914KnobSetting = Scp914KnobSetting.Rough;
				}
				Scp914KnobSetting knobSetting = this._knobSetting;
				Scp914KnobChangingEventArgs scp914KnobChangingEventArgs = new Scp914KnobChangingEventArgs(knobSetting, scp914KnobSetting, ply);
				Scp914Events.OnKnobChanging(scp914KnobChangingEventArgs);
				if (!scp914KnobChangingEventArgs.IsAllowed)
				{
					return;
				}
				scp914KnobSetting = scp914KnobChangingEventArgs.KnobSetting;
				this.RemainingCooldown = this.KnobChangeCooldown;
				this.Network_knobSetting = scp914KnobSetting;
				this.RpcPlaySound(0);
				Scp914Events.OnKnobChanged(new Scp914KnobChangedEventArgs(knobSetting, scp914KnobSetting, ply));
				return;
			}
		}

		[Server]
		public void Upgrade()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Scp914.Scp914Controller::Upgrade()' called when server was not active");
				return;
			}
			this.RemainingCooldown = this.TotalSequenceTime;
			this.IsUpgrading = true;
			this._itemsAlreadyUpgraded = false;
			this.RpcPlaySound(1);
		}

		private void Start()
		{
			Scp914Controller.Singleton = this;
			if (Scp914Upgrader.SolidObjectMask == 0)
			{
				Scp914Upgrader.SolidObjectMask = LayerMask.GetMask(new string[] { "Default", "Door" });
			}
			if (NetworkServer.active)
			{
				this.ConfigMode = new ConfigEntry<Scp914Mode>("914_mode", Scp914Mode.DroppedAndHeld, "SCP-914 Mode", "The behavior SCP-914 should use when upgrading items.");
				ConfigFile.ServerConfig.RegisterConfig(this.ConfigMode, true);
			}
		}

		private void OnDestroy()
		{
			if (NetworkServer.active)
			{
				ConfigFile.ServerConfig.UnRegisterConfig(this.ConfigMode);
			}
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				this.UpdateServerside();
			}
		}

		[Server]
		private void UpdateServerside()
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void Scp914.Scp914Controller::UpdateServerside()' called when server was not active");
				return;
			}
			if (this.IsUpgrading)
			{
				float num = this.TotalSequenceTime - this.RemainingCooldown;
				if (num >= this.DoorCloseTime && num < this.DoorOpenTime && this.Doors[0].TargetState)
				{
					DoorVariant[] array = this.Doors;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].NetworkTargetState = false;
					}
				}
				else if (num >= this.ItemUpgradeTime && !this._itemsAlreadyUpgraded)
				{
					Scp914Upgrader.Upgrade(Physics.OverlapBox(this.IntakeChamber.position, this.IntakeChamberSize), this.ConfigMode.Value, this._knobSetting);
					this._itemsAlreadyUpgraded = true;
				}
				else if (num >= this.DoorOpenTime && !this.Doors[0].TargetState)
				{
					DoorVariant[] array = this.Doors;
					for (int i = 0; i < array.Length; i++)
					{
						array[i].NetworkTargetState = true;
					}
				}
			}
			if (this.RemainingCooldown >= 0f)
			{
				this.RemainingCooldown -= Time.deltaTime;
				if (this.RemainingCooldown < 0f)
				{
					this.IsUpgrading = false;
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawCube(this.IntakeChamber.position, this.IntakeChamberSize * 2f);
			Gizmos.DrawCube(this.OutputChamber.position, this.IntakeChamberSize * 2f);
		}

		[ClientRpc]
		public void RpcPlaySound(byte soundId)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteByte(soundId);
			this.SendRPCInternal("System.Void Scp914.Scp914Controller::RpcPlaySound(System.Byte)", 1143347406, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		public override bool Weaved()
		{
			return true;
		}

		public Scp914KnobSetting Network_knobSetting
		{
			get
			{
				return this._knobSetting;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<Scp914KnobSetting>(value, ref this._knobSetting, 1UL, null);
			}
		}

		protected void UserCode_RpcPlaySound__Byte(byte soundId)
		{
			if (soundId == 0)
			{
				this._knobSoundSource.Stop();
				this._knobSoundSource.Play();
				return;
			}
			if (soundId != 1)
			{
				return;
			}
			this._upgradingSoundSource.Stop();
			this._upgradingSoundSource.Play();
		}

		protected static void InvokeUserCode_RpcPlaySound__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlaySound called on server.");
				return;
			}
			((Scp914Controller)obj).UserCode_RpcPlaySound__Byte(reader.ReadByte());
		}

		static Scp914Controller()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp914Controller), "System.Void Scp914.Scp914Controller::RpcPlaySound(System.Byte)", new RemoteCallDelegate(Scp914Controller.InvokeUserCode_RpcPlaySound__Byte));
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_Scp914.Scp914KnobSetting(writer, this._knobSetting);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_Scp914.Scp914KnobSetting(writer, this._knobSetting);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<Scp914KnobSetting>(ref this._knobSetting, null, global::Mirror.GeneratedNetworkCode._Read_Scp914.Scp914KnobSetting(reader));
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<Scp914KnobSetting>(ref this._knobSetting, null, global::Mirror.GeneratedNetworkCode._Read_Scp914.Scp914KnobSetting(reader));
			}
		}

		[SyncVar]
		[SerializeField]
		private Scp914KnobSetting _knobSetting;

		[SerializeField]
		private AudioSource _knobSoundSource;

		[SerializeField]
		private AudioSource _upgradingSoundSource;

		[SerializeField]
		private Transform _knobTransform;

		public float KnobChangeCooldown;

		public float TotalSequenceTime;

		public float DoorCloseTime;

		public float ItemUpgradeTime;

		public float DoorOpenTime;

		public DoorVariant[] Doors;

		public Vector3 ChamberSize;

		public bool IsUpgrading;

		public float RemainingCooldown;

		public ConfigEntry<Scp914Mode> ConfigMode;

		private bool _itemsAlreadyUpgraded;
	}
}

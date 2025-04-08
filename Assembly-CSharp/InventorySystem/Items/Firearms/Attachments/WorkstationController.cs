using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using Mirror;
using PlayerRoles;
using UnityEngine;
using UnityEngine.Serialization;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class WorkstationController : NetworkBehaviour, IClientInteractable, IInteractable, IServerInteractable
	{
		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public void ClientInteract(InteractableCollider collider)
		{
			WorkstationSelectorCollider workstationSelectorCollider = collider as WorkstationSelectorCollider;
			if (workstationSelectorCollider != null)
			{
				this._selector.ProcessCollider(workstationSelectorCollider.ColliderId);
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (colliderId == this.ActivateCollider.ColliderId && this.Status == 0)
			{
				this.NetworkStatus = 1;
				this.ServerStopwatch.Restart();
			}
		}

		private void Start()
		{
			WorkstationController.AllWorkstations.Add(this);
		}

		private void OnDestroy()
		{
			WorkstationController.AllWorkstations.Remove(this);
		}

		private void Update()
		{
			if (NetworkServer.active)
			{
				if (this.Status == 0)
				{
					return;
				}
				switch (this.Status)
				{
				case 1:
					if (this.ServerStopwatch.Elapsed.TotalSeconds > 5.599999904632568)
					{
						this.NetworkStatus = 3;
						this.ServerStopwatch.Restart();
					}
					break;
				case 2:
					if (this.ServerStopwatch.Elapsed.TotalSeconds > 2.5)
					{
						this.NetworkStatus = 0;
						this.ServerStopwatch.Stop();
					}
					break;
				case 3:
					if (this.ServerStopwatch.Elapsed.TotalSeconds < 30.0)
					{
						if (this.ServerStopwatch.Elapsed.TotalSeconds <= 5.0)
						{
							break;
						}
						if (this.IsInRange(this.KnownUser))
						{
							this.ServerStopwatch.Restart();
							break;
						}
						using (HashSet<ReferenceHub>.Enumerator enumerator = ReferenceHub.AllHubs.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								ReferenceHub referenceHub = enumerator.Current;
								if (this.IsInRange(referenceHub))
								{
									this.KnownUser = referenceHub;
									this.ServerStopwatch.Restart();
									break;
								}
							}
							break;
						}
					}
					this.NetworkStatus = 2;
					this.ServerStopwatch.Restart();
					break;
				}
			}
			if (this._prevStatus == this.Status)
			{
				return;
			}
			WorkstationController.WorkstationStatus status = (WorkstationController.WorkstationStatus)this.Status;
			this._selector.enabled = status == WorkstationController.WorkstationStatus.Online;
			this._selectorScreen.SetActive(status == WorkstationController.WorkstationStatus.Online);
			this._idleScreen.SetActive(status == WorkstationController.WorkstationStatus.Offline);
			this._powerupScreen.SetActive(status == WorkstationController.WorkstationStatus.PoweringUp);
			this._powerdownScreen.SetActive(status == WorkstationController.WorkstationStatus.PoweringDown);
			this._prevStatus = this.Status;
		}

		public bool IsInRange(ReferenceHub hub)
		{
			return hub != null && hub.IsAlive() && Mathf.Abs(hub.transform.position.y - base.transform.position.y) < 2.4f && Mathf.Abs(hub.transform.position.x - base.transform.position.x) < 2.4f && Mathf.Abs(hub.transform.position.z - base.transform.position.z) < 2.4f;
		}

		public override bool Weaved()
		{
			return true;
		}

		public byte NetworkStatus
		{
			get
			{
				return this.Status;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<byte>(value, ref this.Status, 1UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				writer.WriteByte(this.Status);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				writer.WriteByte(this.Status);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.Status, null, reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this.Status, null, reader.ReadByte());
			}
		}

		public static readonly HashSet<WorkstationController> AllWorkstations = new HashSet<WorkstationController>();

		[SerializeField]
		private WorkstationAttachmentSelector _selector;

		[SerializeField]
		private GameObject _idleScreen;

		[SerializeField]
		private GameObject _powerupScreen;

		[SerializeField]
		private GameObject _powerdownScreen;

		[SerializeField]
		private GameObject _selectorScreen;

		[FormerlySerializedAs("_activateCollder")]
		public InteractableCollider ActivateCollider;

		[SyncVar]
		public byte Status;

		public ReferenceHub KnownUser;

		public readonly Stopwatch ServerStopwatch = new Stopwatch();

		private const float StandbyDistance = 2.4f;

		private const float UpkeepTime = 30f;

		private const float CheckTime = 5f;

		private const float PowerupTime = 5.6f;

		private const float PowerdownTime = 2.5f;

		private byte _prevStatus;

		public enum WorkstationStatus : byte
		{
			Offline,
			PoweringUp,
			PoweringDown,
			Online
		}
	}
}

using System;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using InventorySystem.Searching;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace AdminToys;

public class InvisibleInteractableToy : AdminToyBase, IServerInteractable, IInteractable, ISearchable
{
	public class InteractableToySearchCompletor : ISearchCompletor
	{
		private readonly InvisibleInteractableToy _target;

		private readonly double _maxDistanceSqr;

		public ReferenceHub Hub { get; }

		public InteractableToySearchCompletor(ReferenceHub hub, InvisibleInteractableToy target, double sqrDistance)
		{
			this.Hub = hub;
			this._target = target;
			this._maxDistanceSqr = sqrDistance;
		}

		public bool ValidateStart()
		{
			PlayerSearchingToyEventArgs e = new PlayerSearchingToyEventArgs(this.Hub, this._target);
			PlayerEvents.OnSearchingToy(e);
			if (!e.IsAllowed)
			{
				return false;
			}
			this._target.OnSearching?.Invoke(this.Hub);
			if (!this._target.IsLocked)
			{
				return this.ValidateDistance();
			}
			return false;
		}

		public bool ValidateUpdate()
		{
			return this.ValidateDistance();
		}

		public void Complete()
		{
			this._target.OnSearched?.Invoke(this.Hub);
			PlayerEvents.OnSearchedToy(new PlayerSearchedToyEventArgs(this.Hub, this._target));
		}

		private bool ValidateDistance()
		{
			return (double)(this._target.transform.position - this.Hub.transform.position).sqrMagnitude <= this._maxDistanceSqr;
		}
	}

	public enum ColliderShape
	{
		Box,
		Sphere,
		Capsule
	}

	[SyncVar(hook = "SetCollider")]
	public ColliderShape Shape;

	[SyncVar]
	public float InteractionDuration;

	[SyncVar]
	public bool IsLocked;

	private Collider _collider;

	public override string CommandName => "Interactable";

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public bool CanSearch
	{
		get
		{
			if (this.InteractionDuration > 0f)
			{
				return !this.IsLocked;
			}
			return false;
		}
	}

	public ColliderShape NetworkShape
	{
		get
		{
			return this.Shape;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Shape, 32uL, SetCollider);
		}
	}

	public float NetworkInteractionDuration
	{
		get
		{
			return this.InteractionDuration;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.InteractionDuration, 64uL, null);
		}
	}

	public bool NetworkIsLocked
	{
		get
		{
			return this.IsLocked;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.IsLocked, 128uL, null);
		}
	}

	public event Action<ReferenceHub> OnInteracted;

	public event Action<ReferenceHub> OnSearching;

	public event Action<ReferenceHub> OnSearched;

	public event Action<ReferenceHub> OnSearchAborted;

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		string[] array = arguments.Array;
		this.NetworkShape = ((array.Length > 2 && Enum.TryParse<ColliderShape>(array[2], ignoreCase: true, out var result)) ? result : ColliderShape.Box);
		this.NetworkInteractionDuration = ((array.Length > 3 && float.TryParse(array[3], out var result2)) ? result2 : 0f);
		float result3;
		float num = ((array.Length > 4 && float.TryParse(array[4], out result3)) ? result3 : 1f);
		base.transform.SetPositionAndRotation(admin.PlayerCameraReference.position, admin.PlayerCameraReference.rotation);
		base.transform.localScale = base.transform.localScale * num;
		this.OnInteracted += delegate(ReferenceHub x)
		{
			admin.gameConsoleTransmission.SendToClient($"{x} triggered OnInteraction", "green");
		};
		this.OnSearching += delegate(ReferenceHub x)
		{
			admin.gameConsoleTransmission.SendToClient($"{x} triggered OnSearching", "green");
		};
		this.OnSearched += delegate(ReferenceHub x)
		{
			admin.gameConsoleTransmission.SendToClient($"{x} triggered OnSearched", "green");
		};
		this.OnSearchAborted += delegate(ReferenceHub x)
		{
			admin.gameConsoleTransmission.SendToClient($"{x} triggered OnSearchAborted", "green");
		};
		base.OnSpawned(admin, arguments);
	}

	protected new void Start()
	{
		this.SetCollider(ColliderShape.Box, this.Shape);
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!(this.InteractionDuration > 0f))
		{
			this.OnInteracted?.Invoke(ply);
			PlayerEvents.OnInteractedToy(new PlayerInteractedToyEventArgs(ply, this));
		}
	}

	public ISearchCompletor GetSearchCompletor(SearchCoordinator coordinator, float sqrDistance)
	{
		return new InteractableToySearchCompletor(coordinator.Hub, this, sqrDistance);
	}

	public float SearchTimeForPlayer(ReferenceHub hub)
	{
		return this.InteractionDuration;
	}

	public bool ServerValidateRequest(NetworkConnection source, SearchSessionPipe session)
	{
		if (!this.CanSearch)
		{
			session.Invalidate();
			return false;
		}
		return true;
	}

	public void ServerHandleAbort(ReferenceHub hub)
	{
		this.OnSearchAborted?.Invoke(hub);
		PlayerEvents.OnSearchToyAborted(new PlayerSearchToyAbortedEventArgs(hub, this));
	}

	private void SetCollider(ColliderShape _, ColliderShape newShape)
	{
		if (this._collider != null)
		{
			UnityEngine.Object.Destroy(this._collider);
		}
		switch (newShape)
		{
		case ColliderShape.Box:
			this._collider = base.gameObject.AddComponent<BoxCollider>();
			break;
		case ColliderShape.Sphere:
			this._collider = base.gameObject.AddComponent<SphereCollider>();
			break;
		case ColliderShape.Capsule:
		{
			CapsuleCollider capsuleCollider = base.gameObject.AddComponent<CapsuleCollider>();
			capsuleCollider.height = 2f;
			capsuleCollider.radius = 0.5f;
			this._collider = capsuleCollider;
			break;
		}
		}
	}

	NetworkIdentity ISearchable.get_netIdentity()
	{
		return base.netIdentity;
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			GeneratedNetworkCode._Write_AdminToys_002EInvisibleInteractableToy_002FColliderShape(writer, this.Shape);
			writer.WriteFloat(this.InteractionDuration);
			writer.WriteBool(this.IsLocked);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			GeneratedNetworkCode._Write_AdminToys_002EInvisibleInteractableToy_002FColliderShape(writer, this.Shape);
		}
		if ((base.syncVarDirtyBits & 0x40L) != 0L)
		{
			writer.WriteFloat(this.InteractionDuration);
		}
		if ((base.syncVarDirtyBits & 0x80L) != 0L)
		{
			writer.WriteBool(this.IsLocked);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Shape, SetCollider, GeneratedNetworkCode._Read_AdminToys_002EInvisibleInteractableToy_002FColliderShape(reader));
			base.GeneratedSyncVarDeserialize(ref this.InteractionDuration, null, reader.ReadFloat());
			base.GeneratedSyncVarDeserialize(ref this.IsLocked, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Shape, SetCollider, GeneratedNetworkCode._Read_AdminToys_002EInvisibleInteractableToy_002FColliderShape(reader));
		}
		if ((num & 0x40L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.InteractionDuration, null, reader.ReadFloat());
		}
		if ((num & 0x80L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsLocked, null, reader.ReadBool());
		}
	}
}

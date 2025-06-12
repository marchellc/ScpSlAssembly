using System;
using System.Collections.Generic;
using Interactables.Verification;
using InventorySystem.Items;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Interactables;

public class InteractionCoordinator : NetworkBehaviour
{
	private readonly HashSet<IInteractionBlocker> _blockers = new HashSet<IInteractionBlocker>();

	private ReferenceHub _hub;

	public const float MaxRaycastRange = 50f;

	public static readonly CachedLayerMask RaycastMask;

	public static KeyCode InteractKey { get; private set; }

	public static bool CanDisarmedInteract { get; internal set; }

	public static event Action<InteractableCollider> OnClientInteracted;

	public void AddBlocker(IInteractionBlocker blocker)
	{
		this._blockers.Add(blocker);
	}

	public bool RemoveBlocker(IInteractionBlocker blocker)
	{
		return this._blockers.Remove(blocker);
	}

	public bool AnyBlocker(BlockedInteraction interactions)
	{
		return this.AnyBlocker((IInteractionBlocker x) => x.BlockedInteractions.HasFlagFast(interactions));
	}

	public bool AnyBlocker(Func<IInteractionBlocker, bool> func)
	{
		this._blockers.RemoveWhere((IInteractionBlocker x) => (x is UnityEngine.Object obj && obj == null) || (x?.CanBeCleared ?? true));
		return this._blockers.Any(func);
	}

	private void Start()
	{
		if (base.isLocalPlayer || NetworkServer.active)
		{
			this._hub = ReferenceHub.GetHub(base.gameObject);
		}
		if (base.isLocalPlayer)
		{
			InteractionCoordinator.InteractKey = NewInput.GetKey(ActionName.Interact);
			NewInput.OnKeyModified += OnKeyModified;
		}
	}

	private void OnDestroy()
	{
		NewInput.OnKeyModified -= OnKeyModified;
	}

	private void Update()
	{
		if (base.isLocalPlayer && Input.GetKeyDown(InteractionCoordinator.InteractKey))
		{
			this.ClientInteract();
		}
	}

	private void OnKeyModified(ActionName actionName, KeyCode keyCode)
	{
		if (actionName == ActionName.Interact)
		{
			InteractionCoordinator.InteractKey = keyCode;
		}
	}

	private void ClientInteract()
	{
		if (!this._hub.IsAlive() || !NetworkClient.ready || !Physics.Raycast(MainCameraController.LastForwardRay, out var hitInfo, 50f, InteractionCoordinator.RaycastMask))
		{
			return;
		}
		if (!hitInfo.collider.TryGetComponent<InteractableCollider>(out var component))
		{
			Transform parent = hitInfo.collider.transform.parent;
			if (parent == null || !parent.TryGetComponent<InteractableCollider>(out component))
			{
				return;
			}
		}
		if (component.Target is IInteractable interactable && !(component.Target == null) && InteractionCoordinator.GetSafeRule(interactable).ClientCanInteract(component, hitInfo))
		{
			if (interactable is IClientInteractable clientInteractable)
			{
				clientInteractable.ClientInteract(component);
			}
			InteractionCoordinator.OnClientInteracted?.Invoke(component);
			if (component.Target is NetworkBehaviour networkBehaviour)
			{
				this.CmdServerInteract(networkBehaviour.netIdentity, component.ColliderId);
			}
		}
	}

	private static IVerificationRule GetSafeRule(IInteractable inter)
	{
		return inter.VerificationRule ?? StandardDistanceVerification.Default;
	}

	[Command(channel = 4)]
	private void CmdServerInteract(NetworkIdentity targetInteractable, byte colId)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteNetworkIdentity(targetInteractable);
		NetworkWriterExtensions.WriteByte(writer, colId);
		base.SendCommandInternal("System.Void Interactables.InteractionCoordinator::CmdServerInteract(Mirror.NetworkIdentity,System.Byte)", -1093998769, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	static InteractionCoordinator()
	{
		InteractionCoordinator.RaycastMask = new CachedLayerMask("Default", "Player", "InteractableNoPlayerCollision", "Hitbox", "Glass", "Door", "Fence");
		InteractionCoordinator.OnClientInteracted = delegate
		{
		};
		RemoteProcedureCalls.RegisterCommand(typeof(InteractionCoordinator), "System.Void Interactables.InteractionCoordinator::CmdServerInteract(Mirror.NetworkIdentity,System.Byte)", InvokeUserCode_CmdServerInteract__NetworkIdentity__Byte, requiresAuthority: true);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_CmdServerInteract__NetworkIdentity__Byte(NetworkIdentity targetInteractable, byte colId)
	{
		if (!(targetInteractable == null) && !(this._hub == null) && this._hub.IsAlive() && targetInteractable.TryGetComponent<IServerInteractable>(out var component) && InteractableCollider.TryGetCollider(component, colId, out var res) && InteractionCoordinator.GetSafeRule(component).ServerCanInteract(this._hub, res))
		{
			component.ServerInteract(this._hub, colId);
		}
	}

	protected static void InvokeUserCode_CmdServerInteract__NetworkIdentity__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdServerInteract called on client.");
		}
		else
		{
			((InteractionCoordinator)obj).UserCode_CmdServerInteract__NetworkIdentity__Byte(reader.ReadNetworkIdentity(), NetworkReaderExtensions.ReadByte(reader));
		}
	}
}

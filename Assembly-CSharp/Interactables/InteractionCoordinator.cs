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
	public static KeyCode InteractKey;

	public static RaycastHit LastRaycastHit;

	public static LayerMask InteractRaycastMask;

	private readonly HashSet<IInteractionBlocker> _blockers = new HashSet<IInteractionBlocker>();

	private ReferenceHub _hub;

	public static event Action<InteractableCollider> OnClientInteracted;

	public void AddBlocker(IInteractionBlocker blocker)
	{
		_blockers.Add(blocker);
	}

	public bool RemoveBlocker(IInteractionBlocker blocker)
	{
		return _blockers.Remove(blocker);
	}

	public bool AnyBlocker(BlockedInteraction interactions)
	{
		return AnyBlocker((IInteractionBlocker x) => x.BlockedInteractions.HasFlagFast(interactions));
	}

	public bool AnyBlocker(Func<IInteractionBlocker, bool> func)
	{
		_blockers.RemoveWhere((IInteractionBlocker x) => (x is UnityEngine.Object @object && @object == null) || (x?.CanBeCleared ?? true));
		return _blockers.Any(func);
	}

	private void Start()
	{
		if (base.isLocalPlayer || NetworkServer.active)
		{
			_hub = ReferenceHub.GetHub(base.gameObject);
		}
		if (base.isLocalPlayer)
		{
			CenterScreenRaycast.OnCenterRaycastHit += OnCenterScreenRaycast;
			InteractKey = NewInput.GetKey(ActionName.Interact);
			NewInput.OnKeyModified += OnKeyModified;
			InteractRaycastMask = LayerMask.GetMask("Default", "Player", "InteractableNoPlayerCollision", "Hitbox", "Glass", "Door", "Fence");
		}
	}

	private void OnDestroy()
	{
		NewInput.OnKeyModified -= OnKeyModified;
		CenterScreenRaycast.OnCenterRaycastHit -= OnCenterScreenRaycast;
	}

	private void OnKeyModified(ActionName actionName, KeyCode keyCode)
	{
		if (actionName == ActionName.Interact)
		{
			InteractKey = keyCode;
		}
	}

	private void ClientInteract()
	{
		if (!_hub.IsAlive() || !NetworkClient.ready)
		{
			return;
		}
		if (!LastRaycastHit.collider.TryGetComponent<InteractableCollider>(out var component))
		{
			Transform parent = LastRaycastHit.collider.transform.parent;
			if (parent == null || !parent.TryGetComponent<InteractableCollider>(out component))
			{
				return;
			}
		}
		if (component.Target is IInteractable interactable && !(component.Target == null) && GetSafeRule(interactable).ClientCanInteract(component, LastRaycastHit))
		{
			if (interactable is IClientInteractable clientInteractable)
			{
				clientInteractable.ClientInteract(component);
			}
			InteractionCoordinator.OnClientInteracted?.Invoke(component);
			if (component.Target is NetworkBehaviour networkBehaviour)
			{
				CmdServerInteract(networkBehaviour.netIdentity, component.ColliderId);
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
		SendCommandInternal("System.Void Interactables.InteractionCoordinator::CmdServerInteract(Mirror.NetworkIdentity,System.Byte)", -1093998769, writer, 4);
		NetworkWriterPool.Return(writer);
	}

	private void OnCenterScreenRaycast(RaycastHit hit)
	{
		if (base.isLocalPlayer)
		{
			LastRaycastHit = hit;
			if (Input.GetKeyDown(InteractKey))
			{
				ClientInteract();
			}
		}
	}

	static InteractionCoordinator()
	{
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
		if (!(targetInteractable == null) && !(_hub == null) && _hub.IsAlive() && targetInteractable.TryGetComponent<IServerInteractable>(out var component) && InteractableCollider.TryGetCollider(component, colId, out var res) && GetSafeRule(component).ServerCanInteract(_hub, res))
		{
			component.ServerInteract(_hub, colId);
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

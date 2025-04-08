using System;
using System.Collections.Generic;
using Interactables.Verification;
using InventorySystem.Items;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace Interactables
{
	public class InteractionCoordinator : NetworkBehaviour
	{
		public static event Action<InteractableCollider> OnClientInteracted;

		public void AddBlocker(IInteractionBlocker blocker)
		{
			this._blockers.Add(blocker);
		}

		public bool AnyBlocker(BlockedInteraction interactions)
		{
			return this.AnyBlocker((IInteractionBlocker x) => x.BlockedInteractions.HasFlagFast(interactions));
		}

		public bool AnyBlocker(Func<IInteractionBlocker, bool> func)
		{
			this._blockers.RemoveWhere(delegate(IInteractionBlocker x)
			{
				global::UnityEngine.Object @object = x as global::UnityEngine.Object;
				return (@object != null && @object == null) || x == null || x.CanBeCleared;
			});
			return this._blockers.Any(func);
		}

		private void Start()
		{
			if (base.isLocalPlayer || NetworkServer.active)
			{
				this._hub = ReferenceHub.GetHub(base.gameObject);
			}
			if (!base.isLocalPlayer)
			{
				return;
			}
			CenterScreenRaycast.OnCenterRaycastHit += this.OnCenterScreenRaycast;
			InteractionCoordinator.InteractKey = NewInput.GetKey(ActionName.Interact, KeyCode.None);
			NewInput.OnKeyModified += this.OnKeyModified;
			InteractionCoordinator.InteractRaycastMask = LayerMask.GetMask(new string[] { "Default", "Player", "InteractableNoPlayerCollision", "Hitbox", "Glass", "Door" });
		}

		private void OnDestroy()
		{
			NewInput.OnKeyModified -= this.OnKeyModified;
			CenterScreenRaycast.OnCenterRaycastHit -= this.OnCenterScreenRaycast;
		}

		private void OnKeyModified(ActionName actionName, KeyCode keyCode)
		{
			if (actionName != ActionName.Interact)
			{
				return;
			}
			InteractionCoordinator.InteractKey = keyCode;
		}

		private void ClientInteract()
		{
			if (!this._hub.IsAlive() || !NetworkClient.ready)
			{
				return;
			}
			InteractableCollider interactableCollider;
			if (!InteractionCoordinator.LastRaycastHit.collider.TryGetComponent<InteractableCollider>(out interactableCollider))
			{
				Transform parent = InteractionCoordinator.LastRaycastHit.collider.transform.parent;
				if (parent == null || !parent.TryGetComponent<InteractableCollider>(out interactableCollider))
				{
					return;
				}
			}
			IInteractable interactable = interactableCollider.Target as IInteractable;
			if (interactable == null || interactableCollider.Target == null)
			{
				return;
			}
			if (!InteractionCoordinator.GetSafeRule(interactable).ClientCanInteract(interactableCollider, InteractionCoordinator.LastRaycastHit))
			{
				return;
			}
			IClientInteractable clientInteractable = interactable as IClientInteractable;
			if (clientInteractable != null)
			{
				clientInteractable.ClientInteract(interactableCollider);
			}
			Action<InteractableCollider> onClientInteracted = InteractionCoordinator.OnClientInteracted;
			if (onClientInteracted != null)
			{
				onClientInteracted(interactableCollider);
			}
			NetworkBehaviour networkBehaviour = interactableCollider.Target as NetworkBehaviour;
			if (networkBehaviour != null)
			{
				this.CmdServerInteract(networkBehaviour.netIdentity, interactableCollider.ColliderId);
			}
		}

		private static IVerificationRule GetSafeRule(IInteractable inter)
		{
			return inter.VerificationRule ?? StandardDistanceVerification.Default;
		}

		[Command(channel = 4)]
		private void CmdServerInteract(NetworkIdentity targetInteractable, byte colId)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteNetworkIdentity(targetInteractable);
			networkWriterPooled.WriteByte(colId);
			base.SendCommandInternal("System.Void Interactables.InteractionCoordinator::CmdServerInteract(Mirror.NetworkIdentity,System.Byte)", -1093998769, networkWriterPooled, 4, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		private void OnCenterScreenRaycast(RaycastHit hit)
		{
			if (!base.isLocalPlayer)
			{
				return;
			}
			InteractionCoordinator.LastRaycastHit = hit;
			if (Input.GetKeyDown(InteractionCoordinator.InteractKey))
			{
				this.ClientInteract();
			}
		}

		static InteractionCoordinator()
		{
			InteractionCoordinator.OnClientInteracted = delegate(InteractableCollider interCollider)
			{
			};
			RemoteProcedureCalls.RegisterCommand(typeof(InteractionCoordinator), "System.Void Interactables.InteractionCoordinator::CmdServerInteract(Mirror.NetworkIdentity,System.Byte)", new RemoteCallDelegate(InteractionCoordinator.InvokeUserCode_CmdServerInteract__NetworkIdentity__Byte), true);
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_CmdServerInteract__NetworkIdentity__Byte(NetworkIdentity targetInteractable, byte colId)
		{
			if (targetInteractable == null || this._hub == null)
			{
				return;
			}
			if (!this._hub.IsAlive())
			{
				return;
			}
			IServerInteractable serverInteractable;
			if (!targetInteractable.TryGetComponent<IServerInteractable>(out serverInteractable))
			{
				return;
			}
			InteractableCollider interactableCollider;
			if (!InteractableCollider.TryGetCollider(serverInteractable, colId, out interactableCollider))
			{
				return;
			}
			if (!InteractionCoordinator.GetSafeRule(serverInteractable).ServerCanInteract(this._hub, interactableCollider))
			{
				return;
			}
			serverInteractable.ServerInteract(this._hub, colId);
		}

		protected static void InvokeUserCode_CmdServerInteract__NetworkIdentity__Byte(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkServer.active)
			{
				Debug.LogError("Command CmdServerInteract called on client.");
				return;
			}
			((InteractionCoordinator)obj).UserCode_CmdServerInteract__NetworkIdentity__Byte(reader.ReadNetworkIdentity(), reader.ReadByte());
		}

		public static KeyCode InteractKey;

		public static RaycastHit LastRaycastHit;

		public static LayerMask InteractRaycastMask;

		private readonly HashSet<IInteractionBlocker> _blockers = new HashSet<IInteractionBlocker>();

		private ReferenceHub _hub;
	}
}

using AudioPooling;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace Interactables.Interobjects;

public class ElevatorSquish : NetworkBehaviour
{
	[SerializeField]
	private PitKiller _pitKiller;

	[SerializeField]
	private AudioClip _squishSound;

	[SerializeField]
	private float _squishRange;

	private ElevatorChamber _chamber;

	private GameObject _killerGo;

	private void Awake()
	{
		this._killerGo = this._pitKiller.gameObject;
		this._chamber = base.GetComponent<ElevatorChamber>();
		this._pitKiller.OnEffectApplied += OnSquished;
	}

	private void OnSquished(ReferenceHub hub)
	{
		if (hub.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			this.PlaySquishSound(fpcRole.FpcModule.Position);
		}
	}

	[ClientRpc]
	private void PlaySquishSound(Vector3 position)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteVector3(position);
		this.SendRPCInternal("System.Void Interactables.Interobjects.ElevatorSquish::PlaySquishSound(UnityEngine.Vector3)", -736738208, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void Update()
	{
		this._killerGo.SetActive(this._chamber.DestinationLevel == 0 && this._chamber.CurSequence == ElevatorChamber.ElevatorSequence.Arriving);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_PlaySquishSound__Vector3(Vector3 position)
	{
		AudioSourcePoolManager.PlayAtPosition(this._squishSound, position, this._squishRange);
	}

	protected static void InvokeUserCode_PlaySquishSound__Vector3(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC PlaySquishSound called on server.");
		}
		else
		{
			((ElevatorSquish)obj).UserCode_PlaySquishSound__Vector3(reader.ReadVector3());
		}
	}

	static ElevatorSquish()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(ElevatorSquish), "System.Void Interactables.Interobjects.ElevatorSquish::PlaySquishSound(UnityEngine.Vector3)", InvokeUserCode_PlaySquishSound__Vector3);
	}
}

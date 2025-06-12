using System.Collections.Generic;
using System.Runtime.InteropServices;
using Interactables;
using Interactables.Verification;
using InventorySystem.Items.Usables;
using MapGeneration;
using Mirror;
using PlayerRoles;
using UnityEngine;

public class Coffee : NetworkBehaviour, IClientInteractable, IInteractable, IServerInteractable
{
	private const float DurationPerCharacter = 0.035f;

	private const float MaxDuration = 7f;

	private const float MinDuration = 4f;

	private const float TotalRegenerationTime = 15f;

	private const int TotalHpToRegenerate = 50;

	private static readonly HashSet<ReferenceHub> BlacklistedPlayers = new HashSet<ReferenceHub>();

	[SyncVar(hook = "SyncCoffeeState")]
	public bool IsConsumed;

	[SerializeField]
	private Color _drinkColor;

	[SerializeField]
	private CoffeeTranslation _drinkText;

	[SerializeField]
	private string _author;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	[SerializeField]
	private AnimationCurve _healProgress;

	[SerializeField]
	private Animator _animator;

	[SerializeField]
	private ParticleSystem _particles;

	[SerializeField]
	private AudioSource _audioSource;

	public IVerificationRule VerificationRule { get; } = StandardDistanceVerification.Default;

	public bool NetworkIsConsumed
	{
		get
		{
			return this.IsConsumed;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.IsConsumed, 1uL, SyncCoffeeState);
		}
	}

	private void Awake()
	{
		if (this.IsConsumed)
		{
			this._animator.enabled = true;
			this._particles.Stop();
		}
	}

	private void Start()
	{
		this._meshRenderer.material.color = this._drinkColor;
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (Coffee.BlacklistedPlayers.Add(ply))
		{
			UsableItemsController.GetHandler(ply).ActiveRegenerations.Add(new RegenerationProcess(this._healProgress, 1f / 15f, 50f));
			this.NetworkIsConsumed = true;
		}
	}

	public void ClientInteract(InteractableCollider collider)
	{
	}

	private void SyncCoffeeState(bool wasConsumed, bool isConsumed)
	{
		this._animator.enabled = isConsumed;
		if (isConsumed)
		{
			this._particles.Stop();
			this._audioSource.Play();
		}
		else
		{
			this._particles.Play();
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		PlayerRoleManager.OnRoleChanged += delegate(ReferenceHub hub, PlayerRoleBase _, PlayerRoleBase _)
		{
			Coffee.BlacklistedPlayers.Remove(hub);
		};
		ReferenceHub.OnPlayerRemoved += delegate(ReferenceHub hub)
		{
			Coffee.BlacklistedPlayers.Remove(hub);
		};
		SeedSynchronizer.OnGenerationFinished += delegate
		{
			Coffee.BlacklistedPlayers.Clear();
		};
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
			writer.WriteBool(this.IsConsumed);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.IsConsumed);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsConsumed, SyncCoffeeState, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.IsConsumed, SyncCoffeeState, reader.ReadBool());
		}
	}
}

using AudioPooling;
using InventorySystem.Items;
using InventorySystem.Items.Firearms.Modules;
using Mirror;
using RelativePositioning;
using UnityEngine;
using Utils.Networking;

namespace PlayerRoles.PlayableScps.Scp939.Ripples;

public class FirearmRippleTrigger : RippleTriggerBase
{
	private Scp939FocusAbility _focus;

	private RelativePosition _syncRipplePos;

	private RoleTypeId _syncRoleColor;

	private ReferenceHub _syncPlayer;

	public override void SpawnObject()
	{
		base.SpawnObject();
		AudioModule.OnSoundPlayed += OnFirearmPlayed;
	}

	public override void ResetObject()
	{
		base.ResetObject();
		AudioModule.OnSoundPlayed -= OnFirearmPlayed;
	}

	public override void ServerWriteRpc(NetworkWriter writer)
	{
		base.ServerWriteRpc(writer);
		writer.WriteRelativePosition(_syncRipplePos);
		writer.WriteReferenceHub(_syncPlayer);
		if (!(_focus.State < 1f))
		{
			writer.WriteSByte((sbyte)_syncRoleColor);
		}
	}

	public override void ClientProcessRpc(NetworkReader reader)
	{
		base.ClientProcessRpc(reader);
		Vector3 position = reader.ReadRelativePosition().Position;
		if (reader.TryReadReferenceHub(out var hub))
		{
			OnPlayedRipple(hub);
		}
		base.Player.Play(position, DecodeColor(reader));
	}

	protected override void Awake()
	{
		base.Awake();
		GetSubroutine<Scp939FocusAbility>(out _focus);
	}

	private void OnFirearmPlayed(ItemIdentifier id, PlayerRoleBase shooterRole, PooledAudioSource src)
	{
		if (NetworkServer.active && shooterRole.TryGetOwner(out var hub) && shooterRole is HumanRole humanRole && !CheckVisibility(hub))
		{
			Vector3 position = humanRole.FpcModule.Position;
			float maxDistance = src.Source.maxDistance;
			if (!((position - base.CastRole.FpcModule.Position).sqrMagnitude > maxDistance * maxDistance))
			{
				_syncRipplePos = new RelativePosition(humanRole.FpcModule.Position);
				_syncRoleColor = humanRole.RoleTypeId;
				_syncPlayer = hub;
				ServerSendRpcToObservers();
			}
		}
	}

	private Color DecodeColor(NetworkReader reader)
	{
		if (reader.Position >= reader.Capacity)
		{
			return Color.red;
		}
		if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>((RoleTypeId)reader.ReadSByte(), out var result))
		{
			return Color.red;
		}
		return result.RoleColor;
	}
}

using System;
using AudioPooling;
using InventorySystem.Items.Autosync;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles.PlayableScps.Scp1507;
using UnityEngine;

namespace InventorySystem.Items.FlamingoTapePlayer;

public class TapeItem : AutosyncItem, IItemDescription, IItemNametag, IHolidayItem
{
	private bool _using;

	private double _equipTime;

	private float _remainingDestroy;

	private const float EquipAnimationTime = 0.32f;

	private const float DestroyTime = 7.6f;

	private const float SoundRange = 10f;

	[SerializeField]
	private AudioClip _successSound;

	[SerializeField]
	private AudioClip _failSound;

	[SerializeField]
	private AudioClip[] _useClips;

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	public HolidayType[] TargetHolidays { get; } = new HolidayType[1] { HolidayType.Christmas };

	public override bool AllowHolster => !this._using;

	public override float Weight => 0.35f;

	public static event Action<ushort, bool> OnPlayerTriggered;

	public override void OnEquipped()
	{
		base.OnEquipped();
		this._equipTime = NetworkTime.time;
	}

	public override void EquipUpdate()
	{
		base.EquipUpdate();
		if (NetworkServer.active && this._using)
		{
			this._remainingDestroy -= Time.deltaTime;
			if (!(this._remainingDestroy > 0f))
			{
				this._using = false;
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
			}
		}
		else if (base.IsControllable && !(NetworkTime.time - this._equipTime < 0.3199999928474426) && base.GetActionDown(ActionName.Shoot))
		{
			base.ClientSendCmd();
		}
	}

	public override void ServerProcessCmd(NetworkReader reader)
	{
		base.ServerProcessCmd(reader);
		if (base.IsEquipped && !this._using)
		{
			bool success = Scp1507Spawner.CurState == Scp1507Spawner.State.Idle;
			this._using = true;
			this._remainingDestroy = 7.6f;
			base.ServerSendPublicRpc(delegate(NetworkWriter x)
			{
				x.WriteBool(success);
			});
			if (success)
			{
				Scp1507Spawner.StartSpawning(base.Owner);
			}
		}
	}

	public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
	{
		base.ClientProcessRpcTemplate(reader, serial);
		bool flag = reader.ReadBool();
		TapeItem.OnPlayerTriggered?.Invoke(serial, flag);
		if (InventoryExtensions.TryGetHubHoldingSerial(serial, out var hub))
		{
			AudioClip genericClip = (flag ? this._successSound : this._failSound);
			bool isLocalPlayer = hub.isLocalPlayer;
			Transform parent = hub.transform;
			PlayClip(genericClip, parent, isLocalPlayer);
			AudioClip[] useClips = this._useClips;
			for (int i = 0; i < useClips.Length; i++)
			{
				PlayClip(useClips[i], parent, isLocalPlayer);
			}
		}
		static void PlayClip(AudioClip sound, Transform trackedTransform, bool useSpatial)
		{
			AudioSourcePoolManager.PlayOnTransform(sound, trackedTransform);
		}
	}
}

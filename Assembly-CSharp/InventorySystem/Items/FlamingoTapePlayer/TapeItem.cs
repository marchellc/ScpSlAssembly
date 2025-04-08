using System;
using System.Runtime.CompilerServices;
using AudioPooling;
using InventorySystem.GUI;
using InventorySystem.Items.Autosync;
using MapGeneration.Holidays;
using Mirror;
using PlayerRoles.PlayableScps.Scp1507;
using UnityEngine;

namespace InventorySystem.Items.FlamingoTapePlayer
{
	public class TapeItem : AutosyncItem, IItemDescription, IItemNametag, IHolidayItem
	{
		public static event Action<ushort, bool> OnPlayerTriggered;

		public string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public HolidayType[] TargetHolidays { get; } = new HolidayType[] { HolidayType.Christmas };

		public override bool AllowHolster
		{
			get
			{
				return !this._using;
			}
		}

		public override float Weight
		{
			get
			{
				return 0.35f;
			}
		}

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
				if (this._remainingDestroy > 0f)
				{
					return;
				}
				this._using = false;
				base.OwnerInventory.ServerRemoveItem(base.ItemSerial, null);
				return;
			}
			else
			{
				if (!InventoryGuiController.ItemsSafeForInteraction)
				{
					return;
				}
				if (NetworkTime.time - this._equipTime < 0.3199999928474426)
				{
					return;
				}
				if (!Input.GetKeyDown(NewInput.GetKey(ActionName.Shoot, KeyCode.None)))
				{
					return;
				}
				base.ClientSendCmd(null);
				return;
			}
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!base.IsEquipped || this._using)
			{
				return;
			}
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

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			bool flag = reader.ReadBool();
			Action<ushort, bool> onPlayerTriggered = TapeItem.OnPlayerTriggered;
			if (onPlayerTriggered != null)
			{
				onPlayerTriggered(serial, flag);
			}
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
			{
				return;
			}
			AudioClip audioClip = (flag ? this._successSound : this._failSound);
			bool isLocalPlayer = referenceHub.isLocalPlayer;
			Transform transform = referenceHub.transform;
			TapeItem.<ClientProcessRpcTemplate>g__PlayClip|26_0(audioClip, transform, isLocalPlayer);
			AudioClip[] useClips = this._useClips;
			for (int i = 0; i < useClips.Length; i++)
			{
				TapeItem.<ClientProcessRpcTemplate>g__PlayClip|26_0(useClips[i], transform, isLocalPlayer);
			}
		}

		[CompilerGenerated]
		internal static void <ClientProcessRpcTemplate>g__PlayClip|26_0(AudioClip genericClip, Transform parent, bool useSpatial)
		{
			AudioSourcePoolManager.PlayOnTransform(genericClip, parent, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

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
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AudioPooling;
using InventorySystem.GUI;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace InventorySystem.Items.Coin
{
	public class Coin : AutosyncItem, IItemDescription, IItemNametag
	{
		public override float Weight
		{
			get
			{
				return 0.0025f;
			}
		}

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

		public static event Action<ushort, bool> OnFlipped;

		public override void OnAdded(ItemPickupBase pickup)
		{
			if (!this.IsLocalPlayer)
			{
				return;
			}
			this._activationKeys = new KeyCode[Coin.ActivationKeys.Length];
			for (int i = 0; i < this._activationKeys.Length; i++)
			{
				this._activationKeys[i] = NewInput.GetKey(Coin.ActivationKeys[i], KeyCode.None);
			}
		}

		public override void EquipUpdate()
		{
			if (!this.IsLocalPlayer || !InventoryGuiController.ItemsSafeForInteraction || this._lastUseSw.Elapsed.TotalSeconds < 0.6000000238418579)
			{
				return;
			}
			AnimatedViewmodelBase animatedViewmodelBase = this.ViewModel as AnimatedViewmodelBase;
			if (animatedViewmodelBase != null && animatedViewmodelBase.AnimatorStateInfo(0).tagHash != Coin.IdleHash)
			{
				return;
			}
			if (base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
			{
				return;
			}
			KeyCode[] activationKeys = this._activationKeys;
			for (int i = 0; i < activationKeys.Length; i++)
			{
				if (Input.GetKeyDown(activationKeys[i]))
				{
					this._lastUseSw.Restart();
					new AutosyncCmd(base.ItemId).Send();
					return;
				}
			}
		}

		public override void ClientProcessRpcTemplate(NetworkReader reader, ushort serial)
		{
			base.ClientProcessRpcTemplate(reader, serial);
			bool flag = reader.ReadBool();
			Action<ushort, bool> onFlipped = Coin.OnFlipped;
			if (onFlipped != null)
			{
				onFlipped(serial, flag);
			}
			Coin.FlipTimes[serial] = NetworkTime.time * (double)(flag ? (-1) : 1);
			ReferenceHub referenceHub;
			if (!InventoryExtensions.TryGetHubHoldingSerial(serial, out referenceHub))
			{
				return;
			}
			if (referenceHub.isLocalPlayer)
			{
				AudioSourcePoolManager.Play2D(this._firstpersonFlipSound, 1f, MixerChannel.DefaultSfx, 1f);
				return;
			}
			AudioSourcePoolManager.PlayOnTransform(this._thirdpersonFlipSound, referenceHub.transform, 5.5f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
		}

		internal override void OnTemplateReloaded(bool wasEverLoaded)
		{
			base.OnTemplateReloaded(wasEverLoaded);
			if (wasEverLoaded)
			{
				return;
			}
			CustomNetworkManager.OnClientReady += Coin.FlipTimes.Clear;
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			if (!base.Owner.isLocalPlayer && this._lastUseSw.Elapsed.TotalSeconds < 0.6000000238418579)
			{
				return;
			}
			if (base.Owner.HasBlock(BlockedInteraction.ItemPrimaryAction))
			{
				return;
			}
			bool flag = global::UnityEngine.Random.value >= 0.5f;
			PlayerFlippingCoinEventArgs playerFlippingCoinEventArgs = new PlayerFlippingCoinEventArgs(base.Owner, this, flag);
			PlayerEvents.OnFlippingCoin(playerFlippingCoinEventArgs);
			if (!playerFlippingCoinEventArgs.IsAllowed)
			{
				return;
			}
			flag = playerFlippingCoinEventArgs.IsTails;
			this._lastUseSw.Restart();
			NetworkWriter networkWriter;
			using (new AutosyncRpc(base.ItemId, out networkWriter))
			{
				networkWriter.WriteBool(flag);
			}
			PlayerEvents.OnFlippedCoin(new PlayerFlippedCoinEventArgs(base.Owner, this, flag));
		}

		[SerializeField]
		private AudioClip _firstpersonFlipSound;

		[SerializeField]
		private AudioClip _thirdpersonFlipSound;

		private readonly Stopwatch _lastUseSw = Stopwatch.StartNew();

		private const float RateLimit = 0.6f;

		public static readonly Dictionary<ushort, double> FlipTimes = new Dictionary<ushort, double>();

		private static readonly int IdleHash = Animator.StringToHash("Idle");

		private static readonly ActionName[] ActivationKeys = new ActionName[]
		{
			ActionName.InspectItem,
			ActionName.Shoot,
			ActionName.Zoom
		};

		private KeyCode[] _activationKeys;
	}
}

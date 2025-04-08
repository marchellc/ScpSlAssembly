using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AudioPooling;
using Christmas.Scp2536.Gifts;
using Hints;
using Interactables;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using UnityEngine;

namespace Christmas.Scp2536
{
	public class Scp2536GiftController : NetworkBehaviour, IServerInteractable, IInteractable
	{
		public static List<Scp2536GiftBase> Gifts { get; } = new List<Scp2536GiftBase>
		{
			new Naughty(),
			new Emergency(),
			new Throwables(),
			new KeycardUpgrade(),
			new LastStand(),
			new MedicalItems(),
			new Scp1576(),
			new StarterCard(),
			new TierOneWeapons(),
			new TierTwoWeapons(),
			new TierThreeWeapons(),
			new TierFourWeapons(),
			new NonStandardWeapons(),
			new TapeGift(),
			new CandyBagGift(),
			new BuddyInABox()
		}.OrderBy((Scp2536GiftBase gift) => gift.Urgency).ToList<Scp2536GiftBase>();

		public static event Action OnServerOpenGift;

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public T ServerGetGift<T>() where T : Scp2536GiftBase
		{
			foreach (Scp2536GiftBase scp2536GiftBase in Scp2536GiftController.Gifts)
			{
				T t = scp2536GiftBase as T;
				if (t != null)
				{
					return t;
				}
			}
			return default(T);
		}

		public void ServerGrantRandomGift(ReferenceHub hub)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			for (int i = 0; i < Scp2536GiftController.Gifts.Count; i++)
			{
				Scp2536GiftBase scp2536GiftBase = Scp2536GiftController.Gifts[i];
				if (!scp2536GiftBase.IgnoredByRandomness && scp2536GiftBase.CanBeGranted(hub))
				{
					if (NonStandardWeapons.CanOverrideGift(scp2536GiftBase))
					{
						scp2536GiftBase = this.ServerGetGift<NonStandardWeapons>();
					}
					scp2536GiftBase.ObtainedBy.Add(hub);
					scp2536GiftBase.ServerGrant(hub);
					return;
				}
			}
			this.ServerGetGift<Naughty>().ServerGrant(hub);
		}

		public void ServerPrepareGifts(bool resetRewards = true)
		{
			for (int i = 0; i < this.GiftBoxes.Length; i++)
			{
				this.RpcSetGiftState(i, true);
			}
			if (!resetRewards)
			{
				return;
			}
			foreach (Scp2536GiftBase scp2536GiftBase in Scp2536GiftController.Gifts)
			{
				scp2536GiftBase.Reset();
			}
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			if (!ply.IsHuman())
			{
				return;
			}
			Scp2536GiftController.GiftBox giftBox = this.GiftBoxes[(int)colliderId % this.GiftBoxes.Length];
			if (!giftBox.CanBeOpen)
			{
				return;
			}
			if (ply.inventory.UserInventory.Items.Count >= 8)
			{
				ply.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[]
				{
					new ByteHintParameter(8)
				}, new HintEffect[] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
				return;
			}
			Stopwatch stopwatch;
			if (this._cooldowns.TryGetValue(ply.netId, out stopwatch))
			{
				if (stopwatch.Elapsed.TotalSeconds < 0.699999988079071)
				{
					return;
				}
				stopwatch.Restart();
			}
			else
			{
				this._cooldowns.Add(ply.netId, Stopwatch.StartNew());
			}
			this.RpcSetGiftState((int)colliderId, false);
			Scp2536Controller.Singleton.GiftController.ServerGrantRandomGift(ply);
			giftBox.CanBeOpen = false;
			Action onServerOpenGift = Scp2536GiftController.OnServerOpenGift;
			if (onServerOpenGift == null)
			{
				return;
			}
			onServerOpenGift();
		}

		[ClientRpc]
		private void RpcSetGiftState(int giftIndex, bool isWrapped)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteInt(giftIndex);
			networkWriterPooled.WriteBool(isWrapped);
			this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536GiftController::RpcSetGiftState(System.Int32,System.Boolean)", -1695853183, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		static Scp2536GiftController()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp2536GiftController), "System.Void Christmas.Scp2536.Scp2536GiftController::RpcSetGiftState(System.Int32,System.Boolean)", new RemoteCallDelegate(Scp2536GiftController.InvokeUserCode_RpcSetGiftState__Int32__Boolean));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcSetGiftState__Int32__Boolean(int giftIndex, bool isWrapped)
		{
			Scp2536GiftController.GiftBox giftBox = this.GiftBoxes[giftIndex];
			giftBox.Lid.SetActive(isWrapped);
			giftBox.CanBeOpen = isWrapped;
			if (!isWrapped)
			{
				giftBox.Particles.Play();
				AudioSourcePoolManager.PlayOnTransform(this._openGiftClip, base.transform, 10f, 1f, FalloffType.Exponential, MixerChannel.DefaultSfx, 1f);
				return;
			}
			giftBox.Particles.Stop();
		}

		protected static void InvokeUserCode_RpcSetGiftState__Int32__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				global::UnityEngine.Debug.LogError("RPC RpcSetGiftState called on server.");
				return;
			}
			((Scp2536GiftController)obj).UserCode_RpcSetGiftState__Int32__Boolean(reader.ReadInt(), reader.ReadBool());
		}

		public Scp2536GiftController.GiftBox[] GiftBoxes;

		[SerializeField]
		private AudioClip _openGiftClip;

		private readonly Dictionary<uint, Stopwatch> _cooldowns = new Dictionary<uint, Stopwatch>();

		private const float InteractionCooldown = 0.7f;

		[Serializable]
		public class GiftBox
		{
			public bool CanBeOpen { get; set; }

			public ParticleSystem Particles;

			public GameObject Lid;
		}
	}
}

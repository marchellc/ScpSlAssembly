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

namespace Christmas.Scp2536;

public class Scp2536GiftController : NetworkBehaviour, IServerInteractable, IInteractable
{
	[Serializable]
	public class GiftBox
	{
		public ParticleSystem Particles;

		public GameObject Lid;

		public bool CanBeOpen { get; set; }
	}

	public GiftBox[] GiftBoxes;

	[SerializeField]
	private AudioClip _openGiftClip;

	private readonly Dictionary<uint, Stopwatch> _cooldowns = new Dictionary<uint, Stopwatch>();

	private const float InteractionCooldown = 0.7f;

	public static List<Scp2536GiftBase> Gifts { get; }

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public static event Action OnServerOpenGift;

	public T ServerGetGift<T>() where T : Scp2536GiftBase
	{
		foreach (Scp2536GiftBase gift in Scp2536GiftController.Gifts)
		{
			if (gift is T result)
			{
				return result;
			}
		}
		return null;
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
			this.RpcSetGiftState(i, isWrapped: true);
		}
		if (!resetRewards)
		{
			return;
		}
		foreach (Scp2536GiftBase gift in Scp2536GiftController.Gifts)
		{
			gift.Reset();
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		if (!ply.IsHuman())
		{
			return;
		}
		GiftBox giftBox = this.GiftBoxes[colliderId % this.GiftBoxes.Length];
		if (!giftBox.CanBeOpen)
		{
			return;
		}
		if (ply.inventory.UserInventory.Items.Count >= 8)
		{
			ply.hints.Show(new TranslationHint(HintTranslations.MaxItemsAlreadyReached, new HintParameter[1]
			{
				new ByteHintParameter(8)
			}, new HintEffect[1] { HintEffectPresets.TrailingPulseAlpha(0.5f, 1f, 0.5f, 2f, 0f, 3) }, 2f));
			return;
		}
		if (this._cooldowns.TryGetValue(ply.netId, out var value))
		{
			if (value.Elapsed.TotalSeconds < 0.699999988079071)
			{
				return;
			}
			value.Restart();
		}
		else
		{
			this._cooldowns.Add(ply.netId, Stopwatch.StartNew());
		}
		this.RpcSetGiftState(colliderId, isWrapped: false);
		Scp2536Controller.Singleton.GiftController.ServerGrantRandomGift(ply);
		giftBox.CanBeOpen = false;
		Scp2536GiftController.OnServerOpenGift?.Invoke();
	}

	[ClientRpc]
	private void RpcSetGiftState(int giftIndex, bool isWrapped)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteInt(giftIndex);
		writer.WriteBool(isWrapped);
		this.SendRPCInternal("System.Void Christmas.Scp2536.Scp2536GiftController::RpcSetGiftState(System.Int32,System.Boolean)", -1695853183, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	static Scp2536GiftController()
	{
		Scp2536GiftController.Gifts = new List<Scp2536GiftBase>
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
		}.OrderBy((Scp2536GiftBase gift) => gift.Urgency).ToList();
		RemoteProcedureCalls.RegisterRpc(typeof(Scp2536GiftController), "System.Void Christmas.Scp2536.Scp2536GiftController::RpcSetGiftState(System.Int32,System.Boolean)", InvokeUserCode_RpcSetGiftState__Int32__Boolean);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcSetGiftState__Int32__Boolean(int giftIndex, bool isWrapped)
	{
		GiftBox giftBox = this.GiftBoxes[giftIndex];
		giftBox.Lid.SetActive(isWrapped);
		giftBox.CanBeOpen = isWrapped;
		if (!isWrapped)
		{
			giftBox.Particles.Play();
			AudioSourcePoolManager.PlayOnTransform(this._openGiftClip, base.transform);
		}
		else
		{
			giftBox.Particles.Stop();
		}
	}

	protected static void InvokeUserCode_RpcSetGiftState__Int32__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			UnityEngine.Debug.LogError("RPC RpcSetGiftState called on server.");
		}
		else
		{
			((Scp2536GiftController)obj).UserCode_RpcSetGiftState__Int32__Boolean(reader.ReadInt(), reader.ReadBool());
		}
	}
}

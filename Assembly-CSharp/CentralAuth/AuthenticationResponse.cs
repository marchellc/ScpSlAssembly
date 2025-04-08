using System;
using Cryptography;
using Mirror;
using NorthwoodLib;
using Org.BouncyCastle.Crypto;

namespace CentralAuth
{
	public struct AuthenticationResponse : NetworkMessage
	{
		public AuthenticationToken AuthToken { readonly get; internal set; }

		public BadgeToken BadgeToken { readonly get; internal set; }

		internal AuthenticationResponse(SignedToken signedAuthToken, SignedToken signedBadgeToken, string publicKey, string ecdhPublicKey, byte[] ecdhPublicKeySignature, bool doNotTrack, bool hideBadge)
		{
			this.SignedAuthToken = signedAuthToken;
			this.SignedBadgeToken = signedBadgeToken;
			this.PublicKey = ECDSA.PublicKeyFromString(publicKey);
			this.EcdhPublicKey = ecdhPublicKey;
			this.EcdhPublicKeySignature = ecdhPublicKeySignature;
			this.AuthToken = null;
			this.BadgeToken = null;
			this.DoNotTrack = doNotTrack;
			this.HideBadge = hideBadge;
			this.PublicKeyHash = StringUtils.Base64Encode(Sha.HashToString(Sha.Sha256(publicKey)));
		}

		internal AuthenticationResponse(SignedToken signedAuthToken, SignedToken signedBadgeToken, AsymmetricKeyParameter publicKey, string ecdhPublicKey, byte[] ecdhPublicKeySignature, bool doNotTrack, bool hideBadge)
		{
			this.SignedAuthToken = signedAuthToken;
			this.SignedBadgeToken = signedBadgeToken;
			this.PublicKey = publicKey;
			this.EcdhPublicKey = ecdhPublicKey;
			this.EcdhPublicKeySignature = ecdhPublicKeySignature;
			this.AuthToken = null;
			this.BadgeToken = null;
			this.DoNotTrack = doNotTrack;
			this.HideBadge = hideBadge;
			this.PublicKeyHash = null;
		}

		public readonly SignedToken SignedAuthToken;

		public readonly SignedToken SignedBadgeToken;

		public readonly AsymmetricKeyParameter PublicKey;

		public readonly string EcdhPublicKey;

		public readonly byte[] EcdhPublicKeySignature;

		public readonly bool DoNotTrack;

		public readonly bool HideBadge;

		public readonly string PublicKeyHash;
	}
}

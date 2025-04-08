using System;
using Cryptography;
using Mirror;

namespace CentralAuth
{
	public static class AuthenticationResponseFunctions
	{
		public static void SerializeAuthenticationResponse(this NetworkWriter writer, AuthenticationResponse value)
		{
			AuthenticationResponseFunctions.AuthenticationResponseFlags authenticationResponseFlags = (AuthenticationResponseFunctions.AuthenticationResponseFlags)0;
			if (value.SignedAuthToken != null)
			{
				authenticationResponseFlags |= AuthenticationResponseFunctions.AuthenticationResponseFlags.AuthToken;
			}
			if (value.SignedBadgeToken != null)
			{
				authenticationResponseFlags |= AuthenticationResponseFunctions.AuthenticationResponseFlags.BadgeToken;
			}
			if (value.DoNotTrack)
			{
				authenticationResponseFlags |= AuthenticationResponseFunctions.AuthenticationResponseFlags.DoNotTrack;
			}
			if (value.HideBadge)
			{
				authenticationResponseFlags |= AuthenticationResponseFunctions.AuthenticationResponseFlags.HideBadge;
			}
			writer.WriteByte((byte)authenticationResponseFlags);
			SignedToken signedAuthToken = value.SignedAuthToken;
			if (signedAuthToken != null)
			{
				signedAuthToken.Serialize(writer);
			}
			SignedToken signedBadgeToken = value.SignedBadgeToken;
			if (signedBadgeToken != null)
			{
				signedBadgeToken.Serialize(writer);
			}
			writer.WriteString(ECDSA.KeyToString(value.PublicKey));
			writer.WriteString(value.EcdhPublicKey);
			writer.WriteArray(value.EcdhPublicKeySignature);
		}

		public static AuthenticationResponse DeserializeAuthenticationResponse(this NetworkReader reader)
		{
			AuthenticationResponseFunctions.AuthenticationResponseFlags authenticationResponseFlags = (AuthenticationResponseFunctions.AuthenticationResponseFlags)reader.ReadByte();
			return new AuthenticationResponse(authenticationResponseFlags.HasFlagFast(AuthenticationResponseFunctions.AuthenticationResponseFlags.AuthToken) ? SignedToken.Deserialize(reader) : null, authenticationResponseFlags.HasFlagFast(AuthenticationResponseFunctions.AuthenticationResponseFlags.BadgeToken) ? SignedToken.Deserialize(reader) : null, reader.ReadString(), reader.ReadString(), reader.ReadArray<byte>(), authenticationResponseFlags.HasFlagFast(AuthenticationResponseFunctions.AuthenticationResponseFlags.DoNotTrack), authenticationResponseFlags.HasFlagFast(AuthenticationResponseFunctions.AuthenticationResponseFlags.HideBadge));
		}

		private static bool HasFlagFast(this AuthenticationResponseFunctions.AuthenticationResponseFlags flags, AuthenticationResponseFunctions.AuthenticationResponseFlags flag)
		{
			return (flags & flag) == flag;
		}

		[Flags]
		private enum AuthenticationResponseFlags : byte
		{
			AuthToken = 1,
			BadgeToken = 2,
			DoNotTrack = 4,
			HideBadge = 8
		}
	}
}

using System;
using System.Collections.Generic;
using Authenticator;
using Utf8Json.Formatters;
using Utf8Json.Formatters.Authenticator;

namespace Utf8Json.Resolvers;

internal static class GeneratedResolverGetFormatterHelper
{
	private static readonly Dictionary<Type, int> lookup;

	static GeneratedResolverGetFormatterHelper()
	{
		GeneratedResolverGetFormatterHelper.lookup = new Dictionary<Type, int>(35)
		{
			{
				typeof(ServerListItem[]),
				0
			},
			{
				typeof(List<string>),
				1
			},
			{
				typeof(NewsListItem[]),
				2
			},
			{
				typeof(DiscordEmbedField[]),
				3
			},
			{
				typeof(DiscordEmbed[]),
				4
			},
			{
				typeof(List<AuthenticatorPlayerObject>),
				5
			},
			{
				typeof(CreditsListMember[]),
				6
			},
			{
				typeof(CreditsListCategory[]),
				7
			},
			{
				typeof(AuthenticatiorAuthReject[]),
				8
			},
			{
				typeof(ServerListItem),
				9
			},
			{
				typeof(ServerList),
				10
			},
			{
				typeof(PlayerListSerialized),
				11
			},
			{
				typeof(NewsListItem),
				12
			},
			{
				typeof(NewsList),
				13
			},
			{
				typeof(DiscordEmbedField),
				14
			},
			{
				typeof(DiscordEmbed),
				15
			},
			{
				typeof(DiscordWebhook),
				16
			},
			{
				typeof(AuthenticatorPlayerObject),
				17
			},
			{
				typeof(AuthenticatorPlayerObjects),
				18
			},
			{
				typeof(CreditsListMember),
				19
			},
			{
				typeof(CreditsListCategory),
				20
			},
			{
				typeof(CreditsList),
				21
			},
			{
				typeof(AuthenticatiorAuthReject),
				22
			},
			{
				typeof(AuthenticatorResponse),
				23
			},
			{
				typeof(NewsRaw),
				24
			},
			{
				typeof(PublicKeyResponse),
				25
			},
			{
				typeof(RenewResponse),
				26
			},
			{
				typeof(ServerListSigned),
				27
			},
			{
				typeof(TranslationManifest),
				28
			},
			{
				typeof(AuthenticateResponse),
				29
			},
			{
				typeof(AuthenticationToken),
				30
			},
			{
				typeof(BadgeToken),
				31
			},
			{
				typeof(SignedToken),
				32
			},
			{
				typeof(RequestSignatureResponse),
				33
			},
			{
				typeof(Token),
				34
			}
		};
	}

	internal static object GetFormatter(Type t)
	{
		if (!GeneratedResolverGetFormatterHelper.lookup.TryGetValue(t, out var value))
		{
			return null;
		}
		return value switch
		{
			0 => new ArrayFormatter<ServerListItem>(), 
			1 => new ListFormatter<string>(), 
			2 => new ArrayFormatter<NewsListItem>(), 
			3 => new ArrayFormatter<DiscordEmbedField>(), 
			4 => new ArrayFormatter<DiscordEmbed>(), 
			5 => new ListFormatter<AuthenticatorPlayerObject>(), 
			6 => new ArrayFormatter<CreditsListMember>(), 
			7 => new ArrayFormatter<CreditsListCategory>(), 
			8 => new ArrayFormatter<AuthenticatiorAuthReject>(), 
			9 => new ServerListItemFormatter(), 
			10 => new ServerListFormatter(), 
			11 => new PlayerListSerializedFormatter(), 
			12 => new NewsListItemFormatter(), 
			13 => new NewsListFormatter(), 
			14 => new DiscordEmbedFieldFormatter(), 
			15 => new DiscordEmbedFormatter(), 
			16 => new DiscordWebhookFormatter(), 
			17 => new AuthenticatorPlayerObjectFormatter(), 
			18 => new AuthenticatorPlayerObjectsFormatter(), 
			19 => new CreditsListMemberFormatter(), 
			20 => new CreditsListCategoryFormatter(), 
			21 => new CreditsListFormatter(), 
			22 => new AuthenticatiorAuthRejectFormatter(), 
			23 => new AuthenticatorResponseFormatter(), 
			24 => new NewsRawFormatter(), 
			25 => new PublicKeyResponseFormatter(), 
			26 => new RenewResponseFormatter(), 
			27 => new ServerListSignedFormatter(), 
			28 => new TranslationManifestFormatter(), 
			29 => new AuthenticateResponseFormatter(), 
			30 => new AuthenticationTokenFormatter(), 
			31 => new BadgeTokenFormatter(), 
			32 => new SignedTokenFormatter(), 
			33 => new RequestSignatureResponseFormatter(), 
			34 => new TokenFormatter(), 
			_ => null, 
		};
	}
}

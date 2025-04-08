using System;
using System.Collections.Generic;
using Authenticator;
using Utf8Json.Formatters;
using Utf8Json.Formatters.Authenticator;

namespace Utf8Json.Resolvers
{
	internal static class GeneratedResolverGetFormatterHelper
	{
		internal static object GetFormatter(Type t)
		{
			int num;
			if (!GeneratedResolverGetFormatterHelper.lookup.TryGetValue(t, out num))
			{
				return null;
			}
			switch (num)
			{
			case 0:
				return new ArrayFormatter<ServerListItem>();
			case 1:
				return new ListFormatter<string>();
			case 2:
				return new ArrayFormatter<NewsListItem>();
			case 3:
				return new ArrayFormatter<DiscordEmbedField>();
			case 4:
				return new ArrayFormatter<DiscordEmbed>();
			case 5:
				return new ListFormatter<AuthenticatorPlayerObject>();
			case 6:
				return new ArrayFormatter<CreditsListMember>();
			case 7:
				return new ArrayFormatter<CreditsListCategory>();
			case 8:
				return new ArrayFormatter<AuthenticatiorAuthReject>();
			case 9:
				return new ServerListItemFormatter();
			case 10:
				return new ServerListFormatter();
			case 11:
				return new PlayerListSerializedFormatter();
			case 12:
				return new NewsListItemFormatter();
			case 13:
				return new NewsListFormatter();
			case 14:
				return new DiscordEmbedFieldFormatter();
			case 15:
				return new DiscordEmbedFormatter();
			case 16:
				return new DiscordWebhookFormatter();
			case 17:
				return new AuthenticatorPlayerObjectFormatter();
			case 18:
				return new AuthenticatorPlayerObjectsFormatter();
			case 19:
				return new CreditsListMemberFormatter();
			case 20:
				return new CreditsListCategoryFormatter();
			case 21:
				return new CreditsListFormatter();
			case 22:
				return new AuthenticatiorAuthRejectFormatter();
			case 23:
				return new AuthenticatorResponseFormatter();
			case 24:
				return new NewsRawFormatter();
			case 25:
				return new PublicKeyResponseFormatter();
			case 26:
				return new RenewResponseFormatter();
			case 27:
				return new ServerListSignedFormatter();
			case 28:
				return new TranslationManifestFormatter();
			case 29:
				return new AuthenticateResponseFormatter();
			case 30:
				return new AuthenticationTokenFormatter();
			case 31:
				return new BadgeTokenFormatter();
			case 32:
				return new SignedTokenFormatter();
			case 33:
				return new RequestSignatureResponseFormatter();
			case 34:
				return new TokenFormatter();
			default:
				return null;
			}
		}

		private static readonly Dictionary<Type, int> lookup = new Dictionary<Type, int>(35)
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
}

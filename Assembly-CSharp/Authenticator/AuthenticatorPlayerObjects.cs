using System;
using System.Collections.Generic;
using Utf8Json;

namespace Authenticator
{
	public readonly struct AuthenticatorPlayerObjects : IEquatable<AuthenticatorPlayerObjects>, IJsonSerializable
	{
		[SerializationConstructor]
		public AuthenticatorPlayerObjects(List<AuthenticatorPlayerObject> objects)
		{
			this.objects = objects;
		}

		public bool Equals(AuthenticatorPlayerObjects other)
		{
			return this.objects == other.objects;
		}

		public override bool Equals(object obj)
		{
			if (obj is AuthenticatorPlayerObjects)
			{
				AuthenticatorPlayerObjects authenticatorPlayerObjects = (AuthenticatorPlayerObjects)obj;
				return this.Equals(authenticatorPlayerObjects);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (this.objects == null)
			{
				return 0;
			}
			return this.objects.GetHashCode();
		}

		public static bool operator ==(AuthenticatorPlayerObjects left, AuthenticatorPlayerObjects right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(AuthenticatorPlayerObjects left, AuthenticatorPlayerObjects right)
		{
			return !left.Equals(right);
		}

		public readonly List<AuthenticatorPlayerObject> objects;
	}
}

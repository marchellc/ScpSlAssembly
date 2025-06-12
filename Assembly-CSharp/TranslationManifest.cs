using System;
using Utf8Json;

public readonly struct TranslationManifest : IEquatable<TranslationManifest>, IJsonSerializable
{
	public readonly string Name;

	public readonly string[] Authors;

	public readonly string[] InterfaceLocales;

	public readonly string[] SystemLocales;

	public readonly string[] ForcedFontOrder;

	[SerializationConstructor]
	public TranslationManifest(string name, string[] authors, string[] interfaceLocales, string[] systemLocales, string[] forcedFontOrder)
	{
		this.Name = name;
		this.Authors = authors;
		this.InterfaceLocales = interfaceLocales;
		this.SystemLocales = systemLocales;
		this.ForcedFontOrder = forcedFontOrder;
	}

	public bool Equals(TranslationManifest other)
	{
		if (string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && this.Authors == other.Authors && this.InterfaceLocales == other.InterfaceLocales && this.SystemLocales == other.SystemLocales)
		{
			return this.ForcedFontOrder == other.ForcedFontOrder;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is TranslationManifest other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((this.Name != null) ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name) : 0) * 397) ^ ((this.Authors != null) ? this.Authors.GetHashCode() : 0)) * 397) ^ ((this.InterfaceLocales != null) ? this.InterfaceLocales.GetHashCode() : 0)) * 397) ^ ((this.SystemLocales != null) ? this.SystemLocales.GetHashCode() : 0)) * 397) ^ ((this.ForcedFontOrder != null) ? this.ForcedFontOrder.GetHashCode() : 0);
	}

	public static bool operator ==(TranslationManifest left, TranslationManifest right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(TranslationManifest left, TranslationManifest right)
	{
		return !left.Equals(right);
	}
}

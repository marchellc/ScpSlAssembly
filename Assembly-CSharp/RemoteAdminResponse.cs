using System;
using System.Buffers.Binary;

public readonly struct RemoteAdminResponse : IEquatable<RemoteAdminResponse>
{
	public RemoteAdminResponse(string content, RemoteAdminResponse.RemoteAdminResponseFlags flags, string overrideDisplay)
	{
		this.Content = (string.IsNullOrEmpty(content) ? null : content);
		this.Flags = flags;
		this.OverrideDisplay = (string.IsNullOrEmpty(overrideDisplay) ? null : overrideDisplay);
	}

	public RemoteAdminResponse(string content, bool isSuccess, bool logInConsole, string overrideDisplay)
	{
		this = new RemoteAdminResponse(content, (isSuccess ? RemoteAdminResponse.RemoteAdminResponseFlags.Successful : ((RemoteAdminResponse.RemoteAdminResponseFlags)0)) | (logInConsole ? RemoteAdminResponse.RemoteAdminResponseFlags.LogInConsole : ((RemoteAdminResponse.RemoteAdminResponseFlags)0)), overrideDisplay);
	}

	public int GetLength
	{
		get
		{
			return ((this.Content == null) ? 0 : Utf8.GetLength(this.Content)) + ((this.OverrideDisplay == null) ? 0 : Utf8.GetLength(this.OverrideDisplay)) + 5;
		}
	}

	public byte[] Serialize()
	{
		byte[] array = new byte[this.GetLength];
		this.Serialize(array);
		return array;
	}

	public void Serialize(byte[] array)
	{
		array[0] = (byte)this.Flags;
		int num = ((this.Content == null) ? 0 : Utf8.GetBytes(this.Content, array, 5));
		BinaryPrimitives.WriteInt32BigEndian(new Span<byte>(array, 1, 4), num);
		if (this.OverrideDisplay != null)
		{
			Utf8.GetBytes(this.OverrideDisplay, array, num + 5);
		}
	}

	public static RemoteAdminResponse Deserialize(byte[] array, int length)
	{
		int num = BinaryPrimitives.ReadInt32BigEndian(new Span<byte>(array, 1, 4));
		string text = ((num > 0) ? Utf8.GetString(array, 5, num) : null);
		string text2 = ((num + 5 < length) ? Utf8.GetString(array, num + 5, length - num - 5) : null);
		return new RemoteAdminResponse(text, (RemoteAdminResponse.RemoteAdminResponseFlags)array[0], text2);
	}

	public bool Equals(RemoteAdminResponse other)
	{
		return string.Equals(this.Content, other.Content) && this.Flags == other.Flags && string.Equals(this.OverrideDisplay, other.OverrideDisplay);
	}

	public override bool Equals(object obj)
	{
		if (obj is RemoteAdminResponse)
		{
			RemoteAdminResponse remoteAdminResponse = (RemoteAdminResponse)obj;
			return this.Equals(remoteAdminResponse);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((this.Content != null) ? this.Content.GetHashCode() : 0) * 397) ^ this.Flags.GetHashCode()) * 397) ^ ((this.OverrideDisplay != null) ? this.OverrideDisplay.GetHashCode() : 0);
	}

	public static bool operator ==(RemoteAdminResponse left, RemoteAdminResponse right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(RemoteAdminResponse left, RemoteAdminResponse right)
	{
		return !left.Equals(right);
	}

	public bool HasFlag(RemoteAdminResponse.RemoteAdminResponseFlags flag)
	{
		return (this.Flags & flag) == flag;
	}

	public readonly string Content;

	public readonly RemoteAdminResponse.RemoteAdminResponseFlags Flags;

	public readonly string OverrideDisplay;

	private const int HeaderSize = 5;

	[Flags]
	public enum RemoteAdminResponseFlags : byte
	{
		Successful = 1,
		LogInConsole = 2
	}
}

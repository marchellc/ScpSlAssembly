using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ServerOutput;

public struct TextOutputEntry : IOutputEntry
{
	public readonly string Text;

	public readonly byte Color;

	public const int Offset = 5;

	private string HexColor
	{
		get
		{
			byte color = this.Color;
			return color.ToString("X");
		}
	}

	public TextOutputEntry(string text, ConsoleColor color)
	{
		this.Text = text;
		this.Color = (byte)color;
	}

	public string GetString()
	{
		return this.HexColor + this.Text;
	}

	public int GetBytesLength()
	{
		return Encoding.UTF8.GetMaxByteCount(this.Text.Length) + 5;
	}

	public void GetBytes(ref byte[] buffer, out int length)
	{
		length = Utf8.GetBytes(this.Text, buffer, 5);
		buffer[0] = this.Color;
		MemoryMarshal.Cast<byte, int>(new ArraySegment<byte>(buffer).Slice(1, 4))[0] = length;
		length += 5;
	}
}

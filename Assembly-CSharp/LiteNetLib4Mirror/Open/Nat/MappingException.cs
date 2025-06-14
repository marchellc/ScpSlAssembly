using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace LiteNetLib4Mirror.Open.Nat;

[Serializable]
public class MappingException : Exception
{
	public int ErrorCode { get; private set; }

	public string ErrorText { get; private set; }

	internal MappingException()
	{
	}

	internal MappingException(string message)
		: base(message)
	{
	}

	internal MappingException(int errorCode, string errorText)
		: base($"Error {errorCode}: {errorText}")
	{
		this.ErrorCode = errorCode;
		this.ErrorText = errorText;
	}

	internal MappingException(string message, Exception innerException)
		: base(message, innerException)
	{
	}

	protected MappingException(SerializationInfo info, StreamingContext context)
		: base(info, context)
	{
	}

	[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
	public override void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		this.ErrorCode = info.GetInt32("errorCode");
		this.ErrorText = info.GetString("errorText");
		base.GetObjectData(info, context);
	}
}

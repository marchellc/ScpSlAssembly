using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Serialization;

internal sealed class CommentsObjectDescriptor : IObjectDescriptor
{
	private readonly IObjectDescriptor _innerDescriptor;

	public string Comment { get; private set; }

	public object Value => this._innerDescriptor.Value;

	public Type Type => this._innerDescriptor.Type;

	public Type StaticType => this._innerDescriptor.StaticType;

	public ScalarStyle ScalarStyle => this._innerDescriptor.ScalarStyle;

	public CommentsObjectDescriptor(IObjectDescriptor innerDescriptor, string comment)
	{
		this._innerDescriptor = innerDescriptor;
		this.Comment = comment;
	}
}

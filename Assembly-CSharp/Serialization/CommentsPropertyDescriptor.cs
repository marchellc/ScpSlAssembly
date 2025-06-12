using System;
using System.ComponentModel;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Serialization;

internal sealed class CommentsPropertyDescriptor : IPropertyDescriptor
{
	private readonly IPropertyDescriptor _baseDescriptor;

	public string Name { get; set; }

	public Type Type => this._baseDescriptor.Type;

	public Type TypeOverride
	{
		get
		{
			return this._baseDescriptor.TypeOverride;
		}
		set
		{
			this._baseDescriptor.TypeOverride = value;
		}
	}

	public int Order { get; set; }

	public ScalarStyle ScalarStyle
	{
		get
		{
			return this._baseDescriptor.ScalarStyle;
		}
		set
		{
			this._baseDescriptor.ScalarStyle = value;
		}
	}

	public bool CanWrite => this._baseDescriptor.CanWrite;

	public CommentsPropertyDescriptor(IPropertyDescriptor baseDescriptor)
	{
		this._baseDescriptor = baseDescriptor;
		this.Name = baseDescriptor.Name;
	}

	public void Write(object target, object value)
	{
		this._baseDescriptor.Write(target, value);
	}

	public T GetCustomAttribute<T>() where T : Attribute
	{
		return this._baseDescriptor.GetCustomAttribute<T>();
	}

	public IObjectDescriptor Read(object target)
	{
		DescriptionAttribute customAttribute = this._baseDescriptor.GetCustomAttribute<DescriptionAttribute>();
		if (customAttribute == null)
		{
			return this._baseDescriptor.Read(target);
		}
		return new CommentsObjectDescriptor(this._baseDescriptor.Read(target), customAttribute.Description);
	}
}

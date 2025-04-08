﻿using System;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Serialization
{
	internal sealed class CommentsObjectDescriptor : IObjectDescriptor
	{
		public CommentsObjectDescriptor(IObjectDescriptor innerDescriptor, string comment)
		{
			this._innerDescriptor = innerDescriptor;
			this.Comment = comment;
		}

		public string Comment { get; private set; }

		public object Value
		{
			get
			{
				return this._innerDescriptor.Value;
			}
		}

		public Type Type
		{
			get
			{
				return this._innerDescriptor.Type;
			}
		}

		public Type StaticType
		{
			get
			{
				return this._innerDescriptor.StaticType;
			}
		}

		public ScalarStyle ScalarStyle
		{
			get
			{
				return this._innerDescriptor.ScalarStyle;
			}
		}

		private readonly IObjectDescriptor _innerDescriptor;
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace Serialization
{
	public class CommentGatheringTypeInspector : TypeInspectorSkeleton
	{
		public CommentGatheringTypeInspector(ITypeInspector innerTypeDescriptor)
		{
			if (innerTypeDescriptor == null)
			{
				throw new ArgumentNullException("innerTypeDescriptor");
			}
			this._innerTypeDescriptor = innerTypeDescriptor;
		}

		public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
		{
			return from descriptor in this._innerTypeDescriptor.GetProperties(type, container)
				select new CommentsPropertyDescriptor(descriptor);
		}

		private readonly ITypeInspector _innerTypeDescriptor;
	}
}

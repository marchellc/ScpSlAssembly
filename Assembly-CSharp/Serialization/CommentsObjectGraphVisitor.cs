﻿using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace Serialization
{
	public class CommentsObjectGraphVisitor : ChainedObjectGraphVisitor
	{
		public CommentsObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
			: base(nextVisitor)
		{
		}

		public override bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context)
		{
			CommentsObjectDescriptor commentsObjectDescriptor = value as CommentsObjectDescriptor;
			if (commentsObjectDescriptor != null && commentsObjectDescriptor.Comment != null)
			{
				context.Emit(new Comment(commentsObjectDescriptor.Comment, false));
			}
			return base.EnterMapping(key, value, context);
		}
	}
}

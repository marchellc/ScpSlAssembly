using System;
using System.Collections.Generic;

namespace CustomPlayerEffects.Danger
{
	public abstract class ParentDangerBase : DangerStackBase
	{
		public override float DangerValue { get; set; }

		private protected List<DangerStackBase> ChildDangers { protected get; private set; } = new List<DangerStackBase>();

		public override bool IsActive
		{
			get
			{
				this.ProcessChildren();
				return this.ChildDangers.Count > 0;
			}
			protected set
			{
				base.IsActive = value;
			}
		}

		private void ProcessChildren()
		{
			this.DangerValue = 0f;
			for (int i = this.ChildDangers.Count - 1; i >= 0; i--)
			{
				if (!this.ChildDangers[i].IsActive)
				{
					this.ChildDangers.RemoveAt(i);
				}
				else
				{
					this.DangerValue += this.ChildDangers[i].DangerValue;
				}
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			this.ChildDangers.Clear();
		}
	}
}

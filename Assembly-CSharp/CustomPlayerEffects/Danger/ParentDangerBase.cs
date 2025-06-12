using System.Collections.Generic;

namespace CustomPlayerEffects.Danger;

public abstract class ParentDangerBase : DangerStackBase
{
	public override float DangerValue { get; set; }

	protected List<DangerStackBase> ChildDangers { get; private set; } = new List<DangerStackBase>();

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
		for (int num = this.ChildDangers.Count - 1; num >= 0; num--)
		{
			if (!this.ChildDangers[num].IsActive)
			{
				this.ChildDangers.RemoveAt(num);
			}
			else
			{
				this.DangerValue += this.ChildDangers[num].DangerValue;
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		this.ChildDangers.Clear();
	}
}

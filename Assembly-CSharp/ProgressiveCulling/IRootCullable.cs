using System;

namespace ProgressiveCulling
{
	public interface IRootCullable : ICullable
	{
		void SetupCache();

		RootCullablePriority Priority { get; }
	}
}

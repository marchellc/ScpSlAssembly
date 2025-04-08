using System;
using System.Text;
using NorthwoodLib.Pools;
using UnityEngine;

namespace Respawning.Announcements
{
	public abstract class WaveAnnouncementBase
	{
		protected virtual float MinGlitch
		{
			get
			{
				return 0.08f;
			}
		}

		protected virtual float MaxGlitch
		{
			get
			{
				return 0.1f;
			}
		}

		protected virtual float MinJam
		{
			get
			{
				return 0.07f;
			}
		}

		protected virtual float MaxJam
		{
			get
			{
				return 0.09f;
			}
		}

		public abstract void CreateAnnouncementString(StringBuilder builder);

		public virtual void PlayAnnouncement()
		{
			float num = (AlphaWarheadController.Detonated ? 2.5f : 1f);
			float num2 = global::UnityEngine.Random.Range(this.MinGlitch, this.MaxGlitch) * num;
			float num3 = global::UnityEngine.Random.Range(this.MinJam, this.MaxJam) * num;
			StringBuilder stringBuilder = StringBuilderPool.Shared.Rent();
			this.CreateAnnouncementString(stringBuilder);
			string text = StringBuilderPool.Shared.ToStringReturn(stringBuilder);
			NineTailedFoxAnnouncer.singleton.ServerOnlyAddGlitchyPhrase(text, num2, num3);
		}

		private const float PostDetonationGlitchMultiplier = 2.5f;

		private const int DefaultGlitchMultiplier = 1;
	}
}

using System;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using PlayerRoles.PlayableScps.Scp079.Rewards;
using PlayerRoles.Subroutines;

namespace PlayerRoles.PlayableScps.Scp079
{
	public abstract class Scp079AbilityBase : StandardSubroutine<Scp079Role>
	{
		private protected Scp079TierManager TierManager { protected get; private set; }

		private protected Scp079AuxManager AuxManager { protected get; private set; }

		private protected Scp079CurrentCameraSync CurrentCamSync { protected get; private set; }

		private protected Scp079LostSignalHandler LostSignalHandler { protected get; private set; }

		private protected Scp079RewardManager RewardManager { protected get; private set; }

		protected override void Awake()
		{
			base.Awake();
			foreach (SubroutineBase subroutineBase in base.CastRole.SubroutineModule.AllSubroutines)
			{
				Scp079TierManager scp079TierManager = subroutineBase as Scp079TierManager;
				if (scp079TierManager != null)
				{
					this.TierManager = scp079TierManager;
				}
				else
				{
					Scp079AuxManager scp079AuxManager = subroutineBase as Scp079AuxManager;
					if (scp079AuxManager != null)
					{
						this.AuxManager = scp079AuxManager;
					}
					else
					{
						Scp079CurrentCameraSync scp079CurrentCameraSync = subroutineBase as Scp079CurrentCameraSync;
						if (scp079CurrentCameraSync != null)
						{
							this.CurrentCamSync = scp079CurrentCameraSync;
						}
						else
						{
							Scp079LostSignalHandler scp079LostSignalHandler = subroutineBase as Scp079LostSignalHandler;
							if (scp079LostSignalHandler != null)
							{
								this.LostSignalHandler = scp079LostSignalHandler;
							}
							else
							{
								Scp079RewardManager scp079RewardManager = subroutineBase as Scp079RewardManager;
								if (scp079RewardManager != null)
								{
									this.RewardManager = scp079RewardManager;
								}
							}
						}
					}
				}
			}
		}
	}
}

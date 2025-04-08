using System;
using PlayerStatsSystem;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl
{
	public class FpcBacktracker : IDisposable
	{
		public float MoveAmount
		{
			get
			{
				return Vector3.Distance(this._newPos, this._prevPos);
			}
		}

		public FpcBacktracker(ReferenceHub attacker, Vector3 claimedPos, Quaternion claimedRot, float backtrack = 0.1f, float forecast = 0.15f)
			: this(attacker, claimedPos, claimedRot, backtrack, forecast, true, true)
		{
		}

		public FpcBacktracker(ReferenceHub target, Vector3 claimedPos, float backtrack = 0.4f)
			: this(target, claimedPos, target.PlayerCameraReference.rotation, backtrack, 0f, true, true)
		{
		}

		public FpcBacktracker(ReferenceHub hub, Vector3 claimedPos, Quaternion claimedRot, float backtrack, float forecast, bool ignoreTp, bool restoreUponDeath)
		{
			IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole != null)
			{
				FirstPersonMovementModule fpcModule = fpcRole.FpcModule;
				this._moved = true;
				this._movedHub = hub;
				this._prevPos = fpcModule.Position;
				this._prevRot = hub.PlayerCameraReference.rotation;
				Bounds bounds = ((backtrack <= 0f) ? new Bounds(fpcModule.Position, Vector3.zero) : fpcModule.Tracer.GenerateBounds(backtrack, ignoreTp));
				if (forecast > 0f)
				{
					bounds.Encapsulate(this._prevPos + fpcModule.Motor.Velocity * forecast);
				}
				this._newPos = bounds.ClosestPoint(claimedPos);
				fpcModule.Position = this._newPos;
				hub.PlayerCameraReference.rotation = claimedRot;
			}
			else
			{
				this._moved = false;
			}
			if (restoreUponDeath)
			{
				PlayerStats.OnAnyPlayerDied += this.OnDied;
				this._restoreUponDeath = true;
				return;
			}
			this._restoreUponDeath = false;
		}

		public void Cancel()
		{
			this._moved = true;
			this._restoreUponDeath = false;
		}

		public void Dispose()
		{
			this.RestorePosition();
		}

		public void RestorePosition()
		{
			if (this._moved)
			{
				IFpcRole fpcRole = this._movedHub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					fpcRole.FpcModule.Position = this._prevPos;
					this._movedHub.PlayerCameraReference.rotation = this._prevRot;
				}
				this._moved = false;
			}
			if (this._restoreUponDeath)
			{
				PlayerStats.OnAnyPlayerDied -= this.OnDied;
				this._restoreUponDeath = false;
			}
		}

		private void OnDied(ReferenceHub ply, DamageHandlerBase dhb)
		{
			if (ply != this._movedHub)
			{
				return;
			}
			this.RestorePosition();
		}

		private readonly Vector3 _prevPos;

		private readonly Quaternion _prevRot;

		private readonly Vector3 _newPos;

		private readonly ReferenceHub _movedHub;

		private bool _moved;

		private bool _restoreUponDeath;
	}
}

using System;
using InventorySystem.Items.Thirdperson;
using InventorySystem.Searching;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.OverlayAnims
{
	public class SearchCompleteAnimation : OverlayAnimationsBase
	{
		public override bool WantsToPlay
		{
			get
			{
				return this._interacted && (this._startTime == null || NetworkTime.time - this._startTime.Value < 1.1109999418258667);
			}
		}

		public override bool Bypassable
		{
			get
			{
				return true;
			}
		}

		public override AnimationClip Clip
		{
			get
			{
				return base.Controller.SearchCompleteClip;
			}
		}

		public override float GetLayerWeight(AnimItemLayer3p layer)
		{
			return 1f;
		}

		public override void OnReassigned()
		{
			base.OnReassigned();
			if (!NetworkServer.active)
			{
				return;
			}
			this._lastSearchCoordinator = base.Model.OwnerHub.searchCoordinator;
			this._lastSearchCoordinator.OnCompleted += this.OnSearchCompleted;
		}

		public override void OnStarted()
		{
			base.OnStarted();
			base.Replay(true);
			this._startTime = new double?(NetworkTime.time);
		}

		public override void OnStopped()
		{
			base.OnStopped();
			this._interacted = false;
			this._startTime = null;
		}

		public override void OnReset()
		{
			base.OnReset();
			if (this._lastSearchCoordinator != null)
			{
				this._lastSearchCoordinator.OnCompleted -= this.OnSearchCompleted;
			}
		}

		public override void ProcessRpc(NetworkReader reader)
		{
			base.ProcessRpc(reader);
			if (base.IsPlaying)
			{
				base.Replay(false);
			}
			this._interacted = true;
		}

		private void OnSearchCompleted(SearchCompletor completor)
		{
			if (!NetworkServer.active)
			{
				return;
			}
			base.SendRpc();
		}

		private bool _interacted;

		private double? _startTime;

		private SearchCoordinator _lastSearchCoordinator;

		private const float ClipLength = 1.111f;
	}
}

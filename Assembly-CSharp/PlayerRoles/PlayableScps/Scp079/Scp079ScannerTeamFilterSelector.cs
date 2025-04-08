using System;
using Mirror;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079ScannerTeamFilterSelector : Scp079AbilityBase
	{
		public Team[] SelectedTeams
		{
			get
			{
				if (this._tempTeams == null)
				{
					this._tempTeams = new Team[this._availableFilters.Length];
				}
				int num = 0;
				for (int i = 0; i < this._availableFilters.Length; i++)
				{
					if (this._selectedTeams[i])
					{
						this._tempTeams[num++] = this._availableFilters[i];
					}
				}
				Team[] array = new Team[num];
				Array.Copy(this._tempTeams, array, num);
				return array;
			}
		}

		public bool AnySelected
		{
			get
			{
				bool[] selectedTeams = this._selectedTeams;
				for (int i = 0; i < selectedTeams.Length; i++)
				{
					if (selectedTeams[i])
					{
						return true;
					}
				}
				return false;
			}
		}

		private int GetTeamIndex(Team team)
		{
			for (int i = 0; i < this._availableFilters.Length; i++)
			{
				if (this._availableFilters[i] == team)
				{
					return i;
				}
			}
			throw new ArgumentOutOfRangeException(string.Format("Team {0} is not a whitelisted breach scanner team", team));
		}

		private void ResetArray()
		{
			int num = this._availableFilters.Length;
			if (this._selectedTeams == null)
			{
				this._selectedTeams = new bool[num];
			}
			for (int i = 0; i < num; i++)
			{
				this._selectedTeams[i] = true;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			this.ResetArray();
		}

		public bool GetTeamStatus(Team team)
		{
			return this._selectedTeams[this.GetTeamIndex(team)];
		}

		public void SetTeamStatus(Team team, bool status)
		{
			this._selectedTeams[this.GetTeamIndex(team)] = status;
			base.ClientSendCmd();
		}

		public override void ClientWriteCmd(NetworkWriter writer)
		{
			base.ClientWriteCmd(writer);
			writer.WriteBoolArray(this._selectedTeams);
		}

		public override void ServerProcessCmd(NetworkReader reader)
		{
			base.ServerProcessCmd(reader);
			reader.ReadBoolArray(this._selectedTeams);
			base.ServerSendRpc(true);
		}

		public override void ServerWriteRpc(NetworkWriter writer)
		{
			base.ServerWriteRpc(writer);
			writer.WriteBoolArray(this._selectedTeams);
		}

		public override void ClientProcessRpc(NetworkReader reader)
		{
			base.ClientProcessRpc(reader);
			if (base.Role.IsLocalPlayer)
			{
				return;
			}
			reader.ReadBoolArray(this._selectedTeams);
		}

		public override void ResetObject()
		{
			base.ResetObject();
			this.ResetArray();
		}

		[SerializeField]
		private Team[] _availableFilters;

		private bool[] _selectedTeams;

		private Team[] _tempTeams;
	}
}

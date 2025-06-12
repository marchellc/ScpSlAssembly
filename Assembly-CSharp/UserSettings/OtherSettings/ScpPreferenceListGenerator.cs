using System.Collections.Generic;
using System.Linq;
using PlayerRoles;
using PlayerRoles.PlayableScps;
using PlayerRoles.RoleAssign;
using TMPro;
using UnityEngine;

namespace UserSettings.OtherSettings;

public class ScpPreferenceListGenerator : MonoBehaviour
{
	[SerializeField]
	private GameObject _template;

	[SerializeField]
	private Transform[] _columns;

	private Transform[] _prevSiblings;

	private IEnumerable<PlayerRoleBase> SpawnableScps => from x in PlayerRoleLoader.AllRoles
		where x.Value is ISpawnableScp
		select x.Value;

	private void Awake()
	{
		this._prevSiblings = new Transform[this._columns.Length];
		int num = 0;
		foreach (PlayerRoleBase item in this.SpawnableScps.OrderBy((PlayerRoleBase x) => x.RoleName))
		{
			int num2 = num++ % this._columns.Length;
			GameObject gameObject = Object.Instantiate(this._template, this._columns[num2]);
			Transform transform = this._prevSiblings[num2];
			if (transform != null)
			{
				gameObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);
			}
			gameObject.GetComponentInChildren<TMP_Text>().text = item.RoleName;
			gameObject.GetComponentInChildren<ScpPreferenceSlider>().SetRole(item.RoleTypeId);
			this._prevSiblings[num2] = gameObject.transform;
		}
	}
}

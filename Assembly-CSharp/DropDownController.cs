using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Dropdown))]
[DisallowMultipleComponent]
public class DropDownController : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	[Tooltip("Indexes that should be ignored. Indexes are 0 based.")]
	public List<int> indexesToDisable = new List<int>();

	private TMP_Dropdown _dropdown;

	private void Awake()
	{
		this._dropdown = base.GetComponent<TMP_Dropdown>();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		Canvas componentInChildren = base.GetComponentInChildren<Canvas>();
		if ((bool)componentInChildren)
		{
			Toggle[] componentsInChildren = componentInChildren.GetComponentsInChildren<Toggle>(includeInactive: true);
			for (int i = 1; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].interactable = !this.indexesToDisable.Contains(i - 1);
			}
		}
	}

	public void EnableOption(int index, bool enable)
	{
		if (index < 1 || index >= this._dropdown.options.Count)
		{
			Debug.LogWarning("Index out of range -> ignored!", this);
			return;
		}
		if (enable)
		{
			if (this.indexesToDisable.Contains(index))
			{
				this.indexesToDisable.Remove(index);
			}
		}
		else if (!this.indexesToDisable.Contains(index))
		{
			this.indexesToDisable.Add(index);
		}
		Canvas componentInChildren = base.GetComponentInChildren<Canvas>();
		if ((bool)componentInChildren)
		{
			componentInChildren.GetComponentsInChildren<Toggle>(includeInactive: true)[index].interactable = enable;
		}
	}

	public void EnableOption(string label, bool enable)
	{
		int num = this._dropdown.options.FindIndex((TMP_Dropdown.OptionData o) => string.Equals(o.text, label));
		this.EnableOption(num + 1, enable);
	}
}

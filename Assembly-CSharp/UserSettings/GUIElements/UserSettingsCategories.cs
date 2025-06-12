using System;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.GUIElements;

public class UserSettingsCategories : MonoBehaviour
{
	[Serializable]
	private struct Category
	{
		public GameObject Group;

		public GameObject Highlight;

		public Button Activator;
	}

	[SerializeField]
	private Category[] _categories;

	[SerializeField]
	private HorizontalLayoutGroup _layoutGroup;

	private int _prev;

	public void ResetSelection()
	{
		this.SelectCategory(0);
	}

	private void Awake()
	{
		for (int i = 0; i < this._categories.Length; i++)
		{
			this.ToggleCategory(i, i == 0);
			int iCopy = i;
			this._categories[i].Activator.onClick.AddListener(delegate
			{
				this.SelectCategory(iCopy);
			});
		}
	}

	private void Update()
	{
		this._layoutGroup.CalculateLayoutInputHorizontal();
		this._layoutGroup.SetLayoutHorizontal();
	}

	private void ToggleCategory(int index, bool isVisible)
	{
		Category category = this._categories[index];
		category.Group.SetActive(isVisible);
		category.Highlight.SetActive(isVisible);
	}

	private void SelectCategory(int cat)
	{
		this.ToggleCategory(this._prev, isVisible: false);
		this.ToggleCategory(cat, isVisible: true);
		this._prev = cat;
	}
}

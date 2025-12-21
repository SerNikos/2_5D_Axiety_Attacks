using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UINavigatorManager : MonoBehaviour
{
	public EventSystem eventSystem;
	public GameObject defaultSelectable;

	private void Start()
	{
		if (eventSystem == null)
			eventSystem = EventSystem.current;
	}

	private void Update()
	{
		GameObject selected = eventSystem.currentSelectedGameObject;

		// Only select default when user presses Tab or arrow keys
		if (selected == null && PressedNavigationKey())
		{
			if (defaultSelectable != null)
				eventSystem.SetSelectedGameObject(defaultSelectable);
			return;
		}

		// Handle keyboard navigation if something is selected
		if (selected != null)
		{
			Selectable selectable = selected.GetComponent<Selectable>();
			if (selectable != null)
			{
				if ((Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift)))
				{
					Selectable previous = selectable.FindSelectableOnUp();
					if (previous != null)
						previous.Select();
				}
				else if (Input.GetKeyDown(KeyCode.Tab))
				{
					Selectable next = selectable.FindSelectableOnDown();
					if (next != null)
						next.Select();
				}
			}
		}
	}

	private bool PressedNavigationKey()
	{
		// Checks if user pressed a key that should trigger selection
		return Input.GetKeyDown(KeyCode.Tab);
	}
}

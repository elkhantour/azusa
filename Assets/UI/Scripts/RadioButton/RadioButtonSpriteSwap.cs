using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drop this on the same GameObject as a RadioButton (or a child).
/// It self-registers into RadioButton.OnSelected / OnDeselected on Awake,
/// so no manual wiring is needed beyond serializing the two sprites.
/// </summary>
[RequireComponent(typeof(RadioButton))]
public class RadioButtonSpriteSwap : MonoBehaviour
{
    [SerializeField] private Image  _target;          // The Image whose sprite will be swapped
    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _selectedSprite;

    private void Awake()
    {
        RadioButton button = GetComponent<RadioButton>();
        button.OnSelected.Add(ShowSelected);
        button.OnDeselected.Add(ShowDeselected);

        // Start in the deselected visual state without going through the group.
        ShowDeselected();
    }

    private void ShowSelected()
    {
        if (_target != null)
            _target.sprite = _selectedSprite;
    }

    private void ShowDeselected()
    {
        if (_target != null)
            _target.sprite = _normalSprite;
    }
}

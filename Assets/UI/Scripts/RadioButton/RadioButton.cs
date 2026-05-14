using UnityEngine;
using UnityEngine.UI;

public class RadioButton : MonoBehaviour
{
    [SerializeField] private Image _background;

    [SerializeField] private Sprite _normalSprite;
    [SerializeField] private Sprite _selectedSprite;

    private RadioGroup _group;

    public void Init(RadioGroup group)
    {
        _group = group;
    }

    public void OnClick()
    {
        _group.Select(this);
    }

    public void SetSelected(bool selected)
    {
        _background.sprite = selected
            ? _selectedSprite
            : _normalSprite;
    }
}

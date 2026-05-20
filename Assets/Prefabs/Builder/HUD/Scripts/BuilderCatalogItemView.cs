using UnityEngine;
using UnityEngine.UI;

namespace Catalog
{
    public class BuilderCatalogItemView : ItemView
    {
        [SerializeField] private Sprite _defaultBackground;
        [SerializeField] private Sprite _activeBackground;

        private Image _backgroundImage;
        private Image _visualImage;

        public override void Init(Item item)
        {
            base.Init(item);

            _visualImage = gameObject.transform.Find("Visual")?.GetComponent<Image>();
            if (_visualImage == null)
            {
                Debug.LogWarning("BuilderCatalogItemView: Could not find 'Visual' Image.");
                return;
            }
            _visualImage.sprite = item.Image;

            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage == null)
            {
                Debug.LogWarning("BuilderCatalogItemView: Could not find 'Background' Image.");
                return;
            }
            _backgroundImage.sprite = _defaultBackground;
        }

        public override void OnSelect()
        {
            if (_backgroundImage != null)
                _backgroundImage.sprite = _activeBackground;
        }

        public override void OnDeselect()
        {
            if (_backgroundImage != null)
                _backgroundImage.sprite = _defaultBackground;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using Catalog;

namespace Island.Builder
{
    public class BuilderCatalogItemView : ItemView
    {

	// used to retrieve the active island
	[SerializeField] private BuilderManager _builderManager; 
        [SerializeField] private BuilderPainter _painter;
        [SerializeField] private Sprite _defaultBackground;
        [SerializeField] private Sprite _activeBackground;

        private Image _backgroundImage;
        private Image _visualImage;

        public override void Init(Item item)
        {
            base.Init(item);

	    if(_builderManager == null || _builderManager.Island == null){
		Debug.LogWarning("BuilderCatalogItemView: Manager or Island not initialized, unable to initialize Painter");
                return;
	    }
	    
	    _painter.Init(_builderManager.Island);

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

            _painter.Enable();
        }

        public override void OnDeselect()
        {
            if (_backgroundImage != null)
                _backgroundImage.sprite = _defaultBackground;

            _painter.Disable();
        }

    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Island
{

    namespace Builder
    {
        public class Catalog : MonoBehaviour
        {

            [Header("Interface Templates")]
            [SerializeField] private GameObject _panel;
            [SerializeField] private GameObject _item;
            private GameObject _content;
            private GameObject _panelInstance;

            [Header("Content")]
            [SerializeField] private List<Asset> _assets;

            private List<GameObject> _items;

            public void Init(GameObject canvas)
            {
                _panelInstance = Instantiate(_panel, canvas.transform);
		_panelInstance.transform.SetAsFirstSibling();
		
                _content = _panelInstance.transform.Find("Viewport/Content")?.gameObject;

                if (_content == null)
                {
                    Debug.LogError("Could not find the Catalog Content Game Object");
                    return;
                }

                PopulateCatalogFromAssets();

            }


            private void PopulateCatalogFromAssets()
            {
                _items.Clear();

                // First add the painter as "edge case"


                // Convert Assets into catalog item
                foreach (Asset asset in _assets)
                {
                    GameObject newItem = Instantiate(_item, _content.transform);
                    newItem.GetComponent<CatalogItem>()?.Init(asset.Name, asset.Image);
                    _items.Add(newItem);
                }


            }

            public void Enable()
            {
                _panelInstance.SetActive(true);
            }

            public void Disable()
            {
                _panelInstance.SetActive(false);
            }
        }

    }

}

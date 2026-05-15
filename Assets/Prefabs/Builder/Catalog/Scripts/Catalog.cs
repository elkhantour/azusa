using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

            private RadioGroup _radioGroup;

            private List<GameObject> _items;

            public void Init(GameObject canvas)
            {
                _panelInstance = Instantiate(_panel, canvas.transform);
                _panelInstance.transform.SetAsFirstSibling();

                _content = _panelInstance.transform.Find("Viewport/Content")?.gameObject;
                _radioGroup = gameObject.AddComponent<RadioGroup>();

                if (_content == null)
                {
                    Debug.LogError("Could not find the Catalog Content Game Object");
                    return;
                }

                UpdateCatalogFromAssets();

            }


            private GameObject SpawnItemFromAsset(Asset asset)
            {
                GameObject newItem = Instantiate(_item, _content.transform);
                newItem.GetComponent<CatalogItem>()?.Init(asset.Name, asset.Image);

                Button button = newItem.GetComponent<Button>() ?? newItem.AddComponent<Button>();

                RadioButton radioButton = newItem.GetComponent<RadioButton>() ?? newItem.AddComponent<RadioButton>();

                _radioGroup.Add(radioButton);

                return newItem;
            }

            private void UpdateCatalogFromAssets()
            {
                _items.Clear();

                // Convert Assets into catalog item
                foreach (Asset asset in _assets)
                {
                    _items.Add(SpawnItemFromAsset(asset));
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

            public void Prepend(Asset asset)
            {
                GameObject newItem = SpawnItemFromAsset(asset);
                newItem.transform.SetAsFirstSibling();
                _items.Insert(0, newItem);

            }

            public void Add(Asset asset)
            {
                _items.Add(SpawnItemFromAsset(asset));
            }
        }

    }

}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
            private Dictionary<RadioButton, Asset> _buttonAssetMap = new Dictionary<RadioButton, Asset>();
            private Asset _activeAsset;
            private List<GameObject> _items;

            public void Init(GameObject canvas)
            {
                _panelInstance = Instantiate(_panel, canvas.transform);
                _panelInstance.transform.SetAsFirstSibling();

                _content = _panelInstance.transform.Find("Viewport/Content")?.gameObject;
                _radioGroup = gameObject.GetComponent<RadioGroup>() ?? gameObject.AddComponent<RadioGroup>();
                _radioGroup.OnSelectionChanged += OnAssetChange;

                if (_content == null)
                {
                    Debug.LogError("Could not find the Catalog Content Game Object");
                    return;
                }

                UpdateCatalogFromAssets();
            }

            /// <summary>
            /// Traverse the assets list and batch assign the same On Active callback to each
            /// list elements
            /// </summary>
            public void BatchOnActiveCallback(UnityAction<Asset> callback)
            {
                foreach (Asset asset in _assets)
                {
                    asset.OnActive.AddListener(callback);
                }
            }

            public void BatchOnInactiveCallback(UnityAction<Asset> callback)
            {
                foreach (Asset asset in _assets)
                {
                    asset.OnInactive.AddListener(callback);
                }
            }

            // TODO: is Asset really necessary? Why not directly use the CatalogItem
            // I am not sure what's the benefit of separating Asset from the Catalog Item
            // If Asset doesn't have any other use case, just merge it with Catalog Item
            private GameObject SpawnItemFromAsset(Asset asset)
            {
                GameObject newItem = Instantiate(_item, _content.transform);
                newItem.GetComponent<CatalogItem>()?.Init(asset.Name, asset.Image);

                Button button = newItem.GetComponent<Button>() ?? newItem.AddComponent<Button>();
                RadioButton radioButton = newItem.GetComponent<RadioButton>() ?? newItem.AddComponent<RadioButton>();
                _buttonAssetMap.Add(radioButton, asset);
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

            public void OnAssetChange(RadioButton changed, RadioButton active)
            {

                if (_activeAsset != null && active == null)
                {
                    _activeAsset.OnInactive.Invoke(_activeAsset);
                    _activeAsset = null;
                }

                if (active != null && _buttonAssetMap.TryGetValue(active, out Asset asset))
                {
                    asset.OnActive.Invoke(asset);
                    _activeAsset = asset;
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

            void OnDestroy()
            {
                Destroy(_radioGroup);
            }
        }

    }

}

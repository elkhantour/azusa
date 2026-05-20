using System.Collections.Generic;
using UnityEngine;

namespace Catalog
{
    public class CatalogController : MonoBehaviour
    {
        [Header("Templates")]
        [SerializeField] private GameObject _panelPrefab;

        [Header("Entries")]
        [SerializeField] public List<CatalogEntry> Entries;

        public GameObject PanelInstance { get; private set; }
        private Transform _content;

        private RadioGroup _radioGroup;

        private readonly Dictionary<RadioButton, ItemView> _buttonMap = new();

        private ItemView _activeView;
        private GameObject _activeWorldInstance;

        public void Init(GameObject canvas)
        {
            PanelInstance = Instantiate(_panelPrefab, canvas.transform);

            _content = PanelInstance.transform.Find("Viewport/Content");

            if (_content == null)
            {
                Debug.LogError("Catalog: 'Viewport/Content' not found in panel prefab.");
                return;
            }

            _radioGroup = GetComponent<RadioGroup>() ?? gameObject.AddComponent<RadioGroup>();
            _radioGroup.OnSelectionChanged += OnSelectionChanged;

            BuildCatalog();
        }

        private void BuildCatalog()
        {
            foreach (var entry in Entries)
            {
                if (entry.Item == null || entry.ViewPrefab == null)
                {
                    Debug.LogWarning("Catalog: skipping entry with missing Item or ViewPrefab.");
                    continue;
                }

                SpawnEntry(entry);
            }
        }

        private void SpawnEntry(CatalogEntry entry)
        {
            // Instantiate the custom view prefab (SimpleImageItemView, ComplexItemView, etc.)
            ItemView view = Instantiate(entry.ViewPrefab, _content);
            view.Init(entry.Item);

            RadioButton radioButton = view.RadioButton;
            _radioGroup.Add(radioButton);
            _buttonMap.Add(radioButton, view);
        }

        private void OnSelectionChanged(RadioButton changed, RadioButton active)
        {
            // Deselect previous
            if (_activeView != null)
                _activeView.OnDeselect();

            // Destroy previous world instance
            if (_activeWorldInstance != null)
            {
                Destroy(_activeWorldInstance);
                _activeWorldInstance = null;
            }

            if (active == null)
            {
                _activeView = null;
                return;
            }

            if (!_buttonMap.TryGetValue(active, out ItemView view))
                return;

            _activeView = view;
            _activeView.OnSelect();

            if (_activeView.Item.WorldPrefab != null)
                _activeWorldInstance = Instantiate(_activeView.Item.WorldPrefab);
        }

        public void Enable()
        {
            if (PanelInstance != null)
                PanelInstance.SetActive(true);
        }

        public void Disable()
        {
            _radioGroup.DisableActive();

            if (_activeWorldInstance != null)
            {
                Destroy(_activeWorldInstance);
                _activeWorldInstance = null;
            }

            _activeView = null;

            if (PanelInstance != null)
                PanelInstance.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_radioGroup != null)
                _radioGroup.OnSelectionChanged -= OnSelectionChanged;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Island
{
    namespace Builder
    {

        public enum ModeType
        {
            Island,
            NomadTown,
            Temple
        }

        public class Mode : MonoBehaviour
        {

            [SerializeField] protected AssetPainter _assetPainter;
            [SerializeField] protected BuilderPainter _customPainter;
            [SerializeField] private Sprite _customPainterSprite;
            [SerializeField] private Catalog _catalog;

            protected Asset _activeAsset;
            protected Island _island;

            public void Init(Island island, GameObject canvas)
            {

                _island = island;

                _catalog.BatchOnActiveCallback(OnAssetActive);
                _catalog.BatchOnInactiveCallback(OnAssetInactive);
                _catalog.Init(canvas);

                if (_assetPainter)
                {
                    _assetPainter.Init(_island);
                    _assetPainter.Disable();
                }

                if (_customPainter)
                {
                    _customPainter.Init(_island);
                    _customPainter.Disable();

                    // TODO update sprite color to: 00548E
                    // Create a new custom asset specifically
                    // for the custom painter and prepend it to the existing catalog
                    Asset asset = new Asset
                    {
                        Name = "Brush",
                        Image = _customPainterSprite
                    };

                    asset.OnActive.AddListener(OnCustomPainterActive);
                    asset.OnInactive.AddListener(OnCustomPainterInactive);

                    _catalog.Prepend(asset);
                }


                Disable();

            }

            public void OnAssetActive(Asset asset)
            {
                Debug.Log($"Enable tool {asset.Name}");
                // TODO update active gameobject
                _assetPainter.Enable();
            }

            public void OnAssetInactive(Asset asset)
            {
                Debug.Log($"Disable tool {asset.Name}");
                _assetPainter.Disable();
            }

            public void OnCustomPainterActive(Asset asset)
            {
                Debug.Log($"Enable tool {asset.Name}");
                _customPainter.Enable();
            }

            public void OnCustomPainterInactive(Asset asset)
            {
                Debug.Log($"Disable tool {asset.Name}");
                _customPainter.Disable();
            }

            public bool Active()
            {
                return enabled;
            }

            public virtual void Disable()
            {
                _catalog.Disable();
                enabled = false;
            }
            public virtual void Enable()
            {
                enabled = true;
                _catalog.Enable();
            }


        }

    }

}

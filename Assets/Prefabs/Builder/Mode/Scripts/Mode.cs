using System;
using System.Collections.Generic;
using UnityEngine;


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

            [SerializeField] protected BuilderPainter _painter;
            [SerializeField] private Catalog _catalog;

            protected Asset _activeAsset;
            protected Island _island;

            public void Init(Island island, GameObject canvas)
            {

                _island = island;

                if (_painter)
                {
                    _painter.Init(_island);
                    _painter.Disable();
                }

                _catalog.Init(canvas);
                Disable();

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

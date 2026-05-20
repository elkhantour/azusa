using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Catalog;


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

            [SerializeField] private CatalogController _catalog;

            protected Island _island;

            public void Init(Island island, GameObject canvas)
            {
                _island = island;
                _catalog.Init(canvas);
		_catalog.PanelInstance.transform.SetAsFirstSibling();
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

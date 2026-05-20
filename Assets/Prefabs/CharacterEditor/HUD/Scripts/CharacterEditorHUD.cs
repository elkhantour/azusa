using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Catalog;

namespace CharacterEditor
{

    public class CharacterEditorHUD : MonoBehaviour
    {

        [SerializeField] private CatalogController _hairCatalog;


        private void Awake()
        {

            _hairCatalog?.Init(gameObject);

        }
    }

}

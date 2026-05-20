using System.Collections.Generic;
using UnityEngine;

namespace Catalog
{
    [CreateAssetMenu(
        fileName = "CatalogDatabase",
        menuName = "Catalog/Catalog Database")]
    public class CatalogDatabase : ScriptableObject
    {
        [field: SerializeField]
        public List<CatalogEntry> Entries { get; private set; }
    }
}

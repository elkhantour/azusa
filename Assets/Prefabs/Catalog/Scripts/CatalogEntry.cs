using UnityEngine;

namespace Catalog
{
    /// <summary>
    /// Pairs a data Item with the specific ItemView prefab that should be
    /// spawned to represent it. The prefab must have a component that
    /// derives from ItemView.
    /// </summary>
    [System.Serializable]
    public class CatalogEntry
    {
        [field: SerializeField] public Item Item { get; private set; }

        /// <summary>
        /// The UI prefab to spawn for this item.
        /// Must have a component derived from ItemView.
        /// </summary>
        [field: SerializeField] public ItemView ViewPrefab { get; private set; }
    }
}

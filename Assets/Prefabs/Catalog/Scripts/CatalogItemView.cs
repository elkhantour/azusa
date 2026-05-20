using UnityEngine;

namespace Catalog
{
    /// <summary>
    /// Base class for all catalog item views.
    /// Create a subclass (e.g. SimpleImageItemView, ComplexItemView) on each
    /// ItemView prefab and override OnSelect / OnDeselect to implement
    /// custom visual behaviour.
    /// </summary>
    [RequireComponent(typeof(RadioButton))]
    public abstract class ItemView : MonoBehaviour
    {
        // Set by the controller after spawning.
        public Item Item { get; private set; }

        // Cached so the controller can register it with the RadioGroup.
        public RadioButton RadioButton { get; private set; }

        protected virtual void Awake()
        {
            RadioButton = GetComponent<RadioButton>();
        }

        /// <summary>Called by CatalogController once after the prefab is spawned.</summary>
        public virtual void Init(Item item)
        {
            Item = item;
        }

        /// <summary>Override to apply your selected visual state.</summary>
        public abstract void OnSelect();

        /// <summary>Override to revert to your default visual state.</summary>
        public abstract void OnDeselect();
    }
}

using UnityEngine;

namespace Catalog
{
    [CreateAssetMenu(menuName = "Catalog/Item")]
    public class Item : ScriptableObject
    {
        public string Name;
        public Sprite Image;
        public GameObject WorldPrefab; // the 3D/world object to spawn on selection
    }
}

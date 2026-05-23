using UnityEngine;

namespace Player
{

    [CreateAssetMenu(menuName = "Items/Outfit")]
    public class Outfit : ScriptableObject
    {
        public WearableItem[] Items;
    }
}

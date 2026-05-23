using UnityEngine;

namespace Player
{

    [CreateAssetMenu(menuName = "Items/Wearable")]
    public class WearableItem : ScriptableObject
    {
        public EquipmentSlot Slot;
        public GameObject VisualPrefab;
        public BodyPart[] HiddenBodyParts;
    }
}

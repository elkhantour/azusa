using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace Player
{


    public enum EquipmentSlot
    {
        Head,
        Top,
        Bottom,
        Shoes,
        Gloves,
        Hair,
        Necklace,
        Bracelet,
        Glasses,
        Earring,
        Belt,
        Strap,
    }


    [Serializable]
    public class EquippedItem
    {
        public WearableItem Item;

        public GameObject SpawnedVisual;
    }

    public class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private PlayerBody _body;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _boneRoot;
        [SerializeField] private Animator _animator;
        [SerializeField] private Outfit _outfit;

        private Dictionary<EquipmentSlot, EquippedItem> _equipped =
            new();


        void Awake()
        {
            if (_outfit != null)
            {
                EquipOutfit(_outfit);
            }
        }

        public void EquipOutfit(Outfit outfit)
        {

            foreach (WearableItem item in outfit.Items)
            {
                Equip(item);
            }

        }

        public bool IsEquipped(EquipmentSlot slot)
        {
            return _equipped.ContainsKey(slot);
        }

        public EquippedItem Get(EquipmentSlot slot)
        {
            _equipped.TryGetValue(slot, out var item);
            return item;
        }

        public void Equip(WearableItem item)
        {
            Unequip(item.Slot);

            var instance = Instantiate(item.VisualPrefab, _visualRoot);

            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

            BindToPlayer(renderer);

            _equipped[item.Slot] = new EquippedItem
            {
                Item = item,
                SpawnedVisual = instance
            };

            foreach (var part in item.HiddenBodyParts)
                _body.SetVisible(part, false);
        }

        public void Unequip(EquipmentSlot slot)
        {
            if (!_equipped.TryGetValue(slot, out var equipped))
                return;

            if (equipped.SpawnedVisual != null)
            {
                Destroy(equipped.SpawnedVisual);
            }

            foreach (var bodyPart in equipped.Item.HiddenBodyParts)
            {
                _body.SetVisible(bodyPart, true);
            }

            _equipped.Remove(slot);
        }

        public void UnequipAll()
        {
            var slots = new List<EquipmentSlot>(_equipped.Keys);

            foreach (var slot in slots)
            {
                Unequip(slot);
            }
        }

        private void BindToPlayer(SkinnedMeshRenderer smr)
        {
            var playerBones = _animator.GetComponentsInChildren<Transform>();
            var map = new Dictionary<string, Transform>();

            foreach (var bone in playerBones)
                map[bone.name] = bone;

            var newBones = new Transform[smr.bones.Length];

            for (int i = 0; i < smr.bones.Length; i++)
            {
                var boneName = smr.bones[i].name;

                if (map.TryGetValue(boneName, out var target))
                    newBones[i] = target;
                else
                    newBones[i] = smr.bones[i];
            }

            smr.bones = newBones;
            smr.rootBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
        }
    }

}

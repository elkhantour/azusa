using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Player;
using System;

namespace CharacterEditor
{
    public class CharacterEditorManager : MonoBehaviour
    {

        [SerializeField] private PlayerBody _playerBody;
        [SerializeField] private PlayerEquipment _playerEquipment;

        // DEBUG 
        [SerializeField] private WearableItem _hair;

        void Awake()
        {
            _playerBody.SetAllVisible(true);
            _playerEquipment.UnequipAll();

	    // DEBUG
	    _playerEquipment.Equip(_hair);
        }

    }
}

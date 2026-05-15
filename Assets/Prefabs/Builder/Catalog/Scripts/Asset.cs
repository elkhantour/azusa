using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;


namespace Island
{

    namespace Builder
    {
        [Serializable]
        public class Asset
        {
            [SerializeField] public string Name;
            [SerializeField] public Sprite Image;
            [SerializeField] public GameObject Object;
            [SerializeField] public UnityEvent<Asset> OnActive = new();
            [SerializeField] public UnityEvent<Asset> OnInactive = new();
        }

    }

}

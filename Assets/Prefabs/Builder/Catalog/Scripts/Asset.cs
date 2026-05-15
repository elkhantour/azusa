using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;


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


        }

    }

}

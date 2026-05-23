using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace Player
{

    [Serializable]
    public class BodyPartEntry
    {
        public BodyPart Part;
        public SkinnedMeshRenderer Renderer;
    }

    public enum BodyPart
    {
        Head,
        TorsoUp,
        TorsoLow,
        Hands,
        ArmsUp,
        ArmsLow,
        LegsUp,
        LegsLow,
        Feet,
        Scalp,
    }

    public class PlayerBody : MonoBehaviour
    {

        [SerializeField]
        private List<BodyPartEntry> _parts;

        private Dictionary<BodyPart, SkinnedMeshRenderer> _lookup = new Dictionary<BodyPart, SkinnedMeshRenderer>();

        private void Awake()
        {
            foreach (var part in _parts)
            {
                if (part.Renderer == null)
                {
                    Debug.LogWarning($"Missing renderer for body part: {part.Part}");
                    continue;
                }

                _lookup[part.Part] = part.Renderer;
            }
        }

        public void SetVisible(BodyPart part, bool visible)
        {
            if (_lookup.TryGetValue(part, out var renderer))
                renderer.enabled = visible;
        }

        public void SetAllVisible(bool visible)
        {
            foreach (var part in _parts)
            {
                if (part.Renderer != null)
                {
                    part.Renderer.enabled = visible;
                }
            }
        }



    }
}

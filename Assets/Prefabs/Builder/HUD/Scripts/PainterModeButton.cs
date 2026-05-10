using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Island
{

    namespace Builder
    {

        namespace HUD
        {

            public class PainterModeButton : MonoBehaviour
            {

                [SerializeField] private Sprite _defaultSprite;
                [SerializeField] private Sprite _activeSprite;
                [SerializeField] private PainterMode _painterMode;

                private Image _image;
                public bool Active = false;

                void Awake()
                {
                    // retrieve Image from the button target graphic
                    _image = GetComponent<Button>().targetGraphic?.GetComponent<Image>();
                }

                public void OnClick()
                {
                    BuilderManager.Instance.UpdatePainterMode(_painterMode);
                }

                public void SetState(bool state)
                {
                    Active = state;
                    _image.sprite = Active ? _activeSprite : _defaultSprite;
                }

            }


        }

    }
}

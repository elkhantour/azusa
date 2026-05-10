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

            public class BuilderHud : MonoBehaviour
            {

                [Header("Painter Modes Triggers")]
                [SerializeField] private List<PainterModeButton> _painterModeButtons = new();
                private PainterModeButton _activeButton;

                public void UpdatePainterButtons(PainterMode mode)
                {
                    PainterModeButton targetButton = _painterModeButtons[(int)mode];

                    if (_activeButton != null && _activeButton != targetButton)
                    {
                        _activeButton.SetState(false);
                    }

                    targetButton.SetState(!targetButton.Active);
                    _activeButton = targetButton.Active ? targetButton : null;

                }

            }

        }
    }
}

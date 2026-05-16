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

                [SerializeField] private GameModeManager _gameModeManager;

                [Header("Painter Modes Triggers")]
                [SerializeField] private List<BuildModeButton> _buildModeButtons = new();

                private BuildModeButton _activeButton;

                public void UpdateModeButtons(ModeType mode)
                {
                    BuildModeButton targetButton = _buildModeButtons[(int)mode];

                    if (_activeButton != null && _activeButton != targetButton)
                    {
                        _activeButton.SetState(false);
                    }

                    targetButton.SetState(!targetButton.Active);
                    _activeButton = targetButton.Active ? targetButton : null;

                }

                public void Discard()
                {
                    _gameModeManager.SetMode(GameMode.Play);
                }

                public void Confirm()
                {
                    _gameModeManager.SetMode(GameMode.Play);
                }

            }

        }
    }
}

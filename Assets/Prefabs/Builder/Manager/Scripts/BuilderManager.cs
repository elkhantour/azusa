using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Island.Builder.HUD;

namespace Island
{

    namespace Builder
    {

        public class BuilderManager : MonoBehaviour
        {

            private static BuilderManager _instance;
            public static BuilderManager Instance
            {
                get
                {
                    return _instance;
                }
            }

            [Header("Build Modes")]
            [SerializeField] private Mode _islandMode;
            [SerializeField] private Mode _nomadTownMode;
            [SerializeField] private Mode _templeMode;

            [Header("Interface")]
            [SerializeField] private GameObject _hudCanvas;
            private BuilderHud _hud;

            private Dictionary<ModeType, Mode> _modes;
            private Mode _activeMode;

            [Header("Output")]
            [SerializeField] private Island _islandPrefab;
            private Island _island;

            void Awake()
            {

                _instance = this;
                _hudCanvas = Instantiate(_hudCanvas);
                _hud = _hudCanvas.GetComponent<BuilderHud>();

                _island = Instantiate(_islandPrefab);
                _island.Init();

                _modes = new Dictionary<ModeType, Mode>
        {
            { ModeType.Island, _islandMode },
            { ModeType.NomadTown, _nomadTownMode },
            { ModeType.Temple, _templeMode },
        };

                foreach (Mode mode in _modes.Values)
                {
                    mode.Init(_island, _hudCanvas);
                }

            }

            private void DisableMode(Mode mode)
            {
                mode.Disable();
                _activeMode = null;
                CameraManager.Instance.UnfreezeZoom();
            }

            private void EnableMode(Mode mode)
            {
                mode.Enable();
                _activeMode = mode;
                CameraManager.Instance.FreezeZoom();
            }

            public void UpdatePainterMode(ModeType type)
            {
                if (_modes.TryGetValue(type, out Mode mode))
                {

                    // Switch mode while one is already active => deactivate active first
                    if (_activeMode && mode != _activeMode)
                    {
                        DisableMode(_activeMode);
                    }


                    if (mode.Active())
                    {
                        DisableMode(mode);
                    }
                    else
                    {
                        EnableMode(mode);
                    }


                }
                else
                {
                    _activeMode = null;
                    CameraManager.Instance.UnfreezeZoom();
                }

                // Update UI Buttons State
                _hud.UpdateModeButtons(type);

            }


        }


    }
}

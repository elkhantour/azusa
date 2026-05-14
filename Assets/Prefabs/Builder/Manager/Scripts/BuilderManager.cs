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


            [Header("Painters")]
            [SerializeField] private IslandPainter _islandPainter;
            [SerializeField] private NomadTownPainter _nomadTownPainter;

            [Header("HUD")]
            [SerializeField] private GameObject _hudCanvas;
            private BuilderHud _hud;

            private Dictionary<PainterMode, BuilderPainter> _painters;
            private BuilderPainter _activePainter;

            [SerializeField] private Island _islandPrefab;
            private Island _island;

            void Awake()
            {

                _instance = this;
                _hudCanvas = Instantiate(_hudCanvas);
                _hud = _hudCanvas.GetComponent<BuilderHud>();

                _island = Instantiate(_islandPrefab);
                _island.Init();

                _painters = new Dictionary<PainterMode, BuilderPainter>
        {
            { PainterMode.Island, _islandPainter },
            { PainterMode.NomadTown, _nomadTownPainter },
        };

                foreach (BuilderPainter pt in _painters.Values)
                {
                    pt.Init(_island);
                    pt.Disable();
                }

            }

            private void DisablePainter(BuilderPainter painter)
            {
                painter.Disable();
                _activePainter = null;
                CameraManager.Instance.UnfreezeZoom();
            }

            private void EnablePainter(BuilderPainter painter)
            {
                painter.Enable();
                _activePainter = painter;
                CameraManager.Instance.FreezeZoom();
            }

            public void UpdatePainterMode(PainterMode mode)
            {
                if (_painters.TryGetValue(mode, out BuilderPainter painter))
                {

                    // Switch mode while one is already active => deactivate active first
                    if (_activePainter && painter != _activePainter)
                    {
                        DisablePainter(_activePainter);
                    }


                    if (painter.enabled)
                    {
                        DisablePainter(painter);
                    }
                    else
                    {
                        EnablePainter(painter);
                    }


                }
                else
                {
                    _activePainter = null;
                    CameraManager.Instance.UnfreezeZoom();
                }

                // Update UI Buttons State
                _hud.UpdatePainterButtons(mode);

            }


        }


    }
}

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

            void Awake()
            {

                _instance = this;
                _hudCanvas = Instantiate(_hudCanvas);
                _hud = _hudCanvas.GetComponent<BuilderHud>();

                _painters = new Dictionary<PainterMode, BuilderPainter>
        {
            { PainterMode.Island, _islandPainter },
            { PainterMode.NomadTown, _nomadTownPainter },
        };

                foreach (BuilderPainter pt in _painters.Values)
                {
                    pt.Disable();
                }
            }

            public void UpdatePainterMode(PainterMode mode)
            {

                if (_painters.TryGetValue(mode, out BuilderPainter painter))
                {
                    if (painter.enabled)
                    {
                        painter.Disable();
                        _activePainter = null;
                    }
                    else
                    {
                        painter.Enable();
                        _activePainter = painter;
                    }

                }
                else
                {
                    _activePainter = null;
                }

                // Update UI Buttons State
                _hud.UpdatePainterButtons(mode);

            }


        }


    }
}

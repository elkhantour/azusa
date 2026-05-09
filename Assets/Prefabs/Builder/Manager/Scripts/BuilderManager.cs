using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


            [SerializeField] private GameObject _hud;
            [SerializeField] private IslandBuilder _islandBuilder;
            [SerializeField] private NomadTownBuilder _nomadTownBuilder;

            private PainterMode _painterMode = PainterMode.Island;

            void Awake()
            {

		_instance = this;
                _hud = Instantiate(_hud);

            }

            public void UpdatePainterMode(PainterMode mode)
            {

                switch (mode)
                {

                    case PainterMode.Island:
                        _islandBuilder.Enable();
                        _nomadTownBuilder.Disable();
                        break;


                    case PainterMode.NomadTown:
                        _islandBuilder.Disable();
                        _nomadTownBuilder.Enable();
                        break;


                    case PainterMode.Temple:
                        _islandBuilder.Disable();
                        _nomadTownBuilder.Disable();
                        break;

                }

            }


        }


    }
}

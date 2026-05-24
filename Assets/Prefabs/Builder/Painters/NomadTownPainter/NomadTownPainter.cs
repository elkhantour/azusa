using System.Collections.Generic;
using UnityEngine;

namespace Island
{

    namespace Builder
    {

        public class NomadTownPainter : BuilderPainter
        {

            [SerializeField] private NomadTownGenerator _townGenerator;
            private Dictionary<GameObject, GameObject> _spawnedTowns = new Dictionary<GameObject, GameObject>();

            protected override void OnPlacementConfirmation()
            {

                float radius = GetActiveChunkRadius();
		
                // If town already exists, detroy and respawn it
                if (_spawnedTowns.TryGetValue(_activeChunk, out GameObject town))
                {
                    Destroy(town);
                }

                GameObject newTown = new GameObject("Nomad Town");

                _townGenerator.GenerateTown(newTown, radius);
                newTown.transform.position = GetActiveChunkPosition();
                _spawnedTowns[_activeChunk] = newTown;

		
                _island.SetNomadTownList(_circlesMask);
		_island.BakeTexture();
		_island.GenerateFlora();
            }


        }

    }
}


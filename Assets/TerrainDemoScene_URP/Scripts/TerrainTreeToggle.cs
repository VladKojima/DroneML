using System;
using UnityEngine;

namespace TerrainDemoScene_URP.Scripts
{
    public class TerrainTreeToggle : MonoBehaviour
    {
        [Obsolete("Obsolete")]
        private void OnEnable()
        {
            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains)
            {
                terrain.drawTreesAndFoliage = false;
            }

            RenderSettings.fogDensity = (float)0.0001;
        }

        [Obsolete("Obsolete")]
        private void OnDisable()
        {
            var terrains = FindObjectsOfType<Terrain>();
            foreach (var terrain in terrains)
            {
                terrain.drawTreesAndFoliage = true;
            }

            RenderSettings.fogDensity = (float)0.0005;
        }
    }
}

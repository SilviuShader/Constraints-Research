using System;
using UnityEngine;

namespace LevelsWFC
{
    [CreateAssetMenu(fileName = "InputExample", menuName = "ScriptableObjects/LevelsWFC/InputExample", order = 1)]
    public class InputExample : ScriptableObject
    {
        [Serializable]
        public struct CellInfo
        {
            public        bool     ContainsTile;
            public        int      TileIndex;

            public static CellInfo Default => new()
            {
                ContainsTile = false,
                TileIndex    = -1
            };
        }

        [HideInInspector]
        public Vector3Int GridSize  = new(10, 10, 10);
        [HideInInspector]
        public CellInfo[] Cells;
        [HideInInspector]
        public Tileset    Tileset;
    }
}
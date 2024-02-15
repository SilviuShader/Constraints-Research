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

            public static CellInfo Default => new()
            {
                ContainsTile = false
            };
        }

        [HideInInspector]
        public Vector3Int GridSize  = new(10, 10, 10);
        [HideInInspector]
        public CellInfo[] Cells;
    }
}
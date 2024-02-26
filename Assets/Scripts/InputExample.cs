using System;
using System.Linq;
using UnityEngine;

namespace LevelsWFC
{
    [CreateAssetMenu(fileName = "InputExample", menuName = "ScriptableObjects/LevelsWFC/InputExample", order = 1)]
    public class InputExample : ScriptableObject
    {
        [Serializable]
        public struct CellInfo : IEquatable<CellInfo>
        {
            public int TileIndex;

            public static CellInfo Default => new()
            {
                TileIndex = -1
            };

            public bool Equals(CellInfo other)
            {
                return TileIndex == other.TileIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is CellInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return TileIndex;
            }
        }
        public bool       Valid     => Cells != null && Cells.All(cell => cell.TileIndex != CellInfo.Default.TileIndex);

        [HideInInspector]
        public Vector3Int GridSize  = new(10, 10, 10);
        [HideInInspector]
        public CellInfo[] Cells;
        [HideInInspector]
        public Tileset    Tileset;

        public static int GridIndex(int w, int d, int h, Vector3Int gridSize) => 
            w * (gridSize.z * gridSize.y) + d * gridSize.y + h;
    }
}
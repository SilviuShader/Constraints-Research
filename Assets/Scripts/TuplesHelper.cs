

using UnityEngine;

namespace LevelsWFC
{
    public static class TuplesHelper
    {
        public static string PairString(Vector2Int pair) => "(" + pair.x + ", " + pair.y + ")";
        public static string PairString(Vector3Int pair) => "(" + pair.x + ", " + pair.z + ", " + pair.y + ")";
    }
}

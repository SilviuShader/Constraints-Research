using UnityEngine;

namespace LevelsWFC
{
    public static class SpawnHelper
    {
        public static Transform SpawnTile(Vector3Int position, Transform tile, Transform parent)
        {
            var (w, d, h) = (position.x, position.z, position.y);
            var newObject = Object.Instantiate(tile, parent);
            newObject.localPosition = new Vector3(w + 0.5f, h + 0.5f, d + 0.5f) * InputEditor.CellSize;
            return newObject;
        }
    }
}
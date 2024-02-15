using UnityEngine;

namespace LevelsWFC
{
    [CreateAssetMenu(fileName = "Tileset", menuName = "ScriptableObjects/LevelsWFC/Tileset", order = 1)]
    public class Tileset : ScriptableObject
    {
        public Transform[] Tiles = null;
    }
}
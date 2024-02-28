using System;
using System.Linq;
using UnityEngine;

namespace LevelsWFC
{
    public class InputEditor : MonoBehaviour
    {
        public enum EditModes
        {
            Paint,
            Delete
        }

        [Serializable]
        public struct TileDictionaryValue 
        {
            public Transform             Transform;
            public InputExample.CellInfo CellInfo;
        }

        [Serializable]
        public class TilesDictionary : SerializableDictionary<Vector3Int, TileDictionaryValue>
        {
        }

        public static float           CellSize       => 1.0f; 
        
        public        InputExample    InputExample   = null;

        [HideInInspector]
        public        int             EditLayer;

        [HideInInspector]
        public        EditModes       EditMode;

        [HideInInspector]
        public        int             PaintTileIndex;

        [SerializeField, HideInInspector]
        private       TilesDictionary _debugTiles    = new();

        [SerializeField, HideInInspector]
        private       bool            _spawnedDebugTiles;

        public void Reset()
        {
            var children = GetComponentsInChildren<Transform>();
            foreach (var child in children)
                if (child != transform)
                    ObjectsHelper.DestroyObjectEditor(child.gameObject);
        }

        public void OnValidate()
        {
            if (InputExample == null)
            {
                PaintTileIndex = 0;
                EditLayer = 0;
                DestroyDebugTiles();
                _spawnedDebugTiles = false;
                return;
            }

            InputExample.GridSize.x = Mathf.Max(0, InputExample.GridSize.x);
            InputExample.GridSize.y = Mathf.Max(0, InputExample.GridSize.y);
            InputExample.GridSize.z = Mathf.Max(0, InputExample.GridSize.z);

            EditLayer = Mathf.Min(EditLayer, InputExample.GridSize.y - 1);
            EditLayer = Mathf.Max(0, EditLayer);

            var targetCellsSize = InputExample.GridSize.x * InputExample.GridSize.y * InputExample.GridSize.z;
            
            if (InputExample.Cells == null || InputExample.Cells.Length == 0)
            {
                InputExample.Cells = new InputExample.CellInfo[targetCellsSize];
                for (var i = 0; i < targetCellsSize; i++)
                    InputExample.Cells[i] = InputExample.CellInfo.Default;
            }

            var maxTileId = 0;
            if (InputExample.Tileset != null)
                if (InputExample.Tileset.Tiles != null)
                    maxTileId = InputExample.Tileset.Tiles.Length - 1;

            for (var i = 0; i < InputExample.Cells.Length; i++)
            {
                if (InputExample.Cells[i].TileIndex == -1)
                    continue;
                if (InputExample.Cells[i].TileIndex <= maxTileId) 
                    continue;
                
                InputExample.Cells[i].TileIndex = -1;
            }

            if (!_spawnedDebugTiles)
            {
                UpdateDebugTiles();
                _spawnedDebugTiles = true;
            }

            if (InputExample.Tileset == null)
                return;

            if (InputExample.Tileset.Tiles is not { Length: > 0 })
                return;

            PaintTileIndex = Mathf.Clamp(PaintTileIndex, 0, InputExample.Tileset.Tiles.Length - 1);
        }

        public void ValidateGridResize(Vector3Int previousSize)
        {
            if (previousSize == InputExample.GridSize)
                return;

            var newArraySize = InputExample.GridSize.x * InputExample.GridSize.y * InputExample.GridSize.z;
            var newCells = new InputExample.CellInfo[newArraySize];

            for (var w = 0; w < InputExample.GridSize.x; w++)
            {
                for (var d = 0; d < InputExample.GridSize.z; d++)
                {
                    for (var h = 0; h < InputExample.GridSize.y; h++)
                    {
                        var cell = InputExample.CellInfo.Default;
                        if (w < previousSize.x && d < previousSize.z && h < previousSize.y)
                        {
                            var oldIndex = InputExample.GridIndex(w, d, h, previousSize);
                            if (oldIndex < InputExample.Cells.Length) 
                                cell = InputExample.Cells[oldIndex];
                        }
                        
                        newCells[InputExample.GridIndex(w, d, h, InputExample.GridSize)] = cell;
                    }
                }
            }

            InputExample.Cells = newCells;
            UpdateDebugTiles();
        }

        public void DestroyDebugTiles()
        {
            var keysToRemove = _debugTiles.Keys.ToArray();
            foreach (var key in keysToRemove)
                DestroyDebugTile(key);
        }

        public void UpdateDebugTiles()
        {
            for (var w = 0; w < InputExample.GridSize.x; w++)
            {
                for (var d = 0; d < InputExample.GridSize.z; d++)
                {
                    for (var h = 0; h < InputExample.GridSize.y; h++)
                    {
                        var key = new Vector3Int(w, h, d);
                        var cell = InputExample.Cells[InputExample.GridIndex(w, d, h, InputExample.GridSize)];
                        TreatDebugCell(key, cell);
                    }
                }
            }

            var toRemoveKeys = _debugTiles.Keys.Where(key => key.x >= InputExample.GridSize.x || key.y >= InputExample.GridSize.y || key.z >= InputExample.GridSize.z).ToList();
            foreach (var key in toRemoveKeys)
                DestroyDebugTile(key);
        }

        public void DestroyDebugTile(Vector3Int key)
        {
            if (!_debugTiles.ContainsKey(key))
                return;

            ObjectsHelper.DestroyObjectEditor(_debugTiles[key].Transform.gameObject);
            _debugTiles.Remove(key);
        }

        public void TreatDebugCell(Vector3Int key, InputExample.CellInfo newCell)
        {
            if (newCell.TileIndex == -1)
            {
                DestroyDebugTile(key);
                return;
            }

            if (_debugTiles.ContainsKey(key))
            {
                var currentValue = _debugTiles[key];

                if (currentValue.CellInfo.Equals(newCell)) 
                    return;

                DestroyDebugTile(key);
            }

            SpawnDebugTile(key, newCell);
        }

        private void SpawnDebugTile(Vector3Int key, InputExample.CellInfo newCell)
        {
            var newObject = SpawnHelper.SpawnTile(key, InputExample.Tileset.Tiles[newCell.TileIndex], transform);
            
            _debugTiles[key] = new TileDictionaryValue
            {
                Transform = newObject,
                CellInfo  = newCell
            };
        }
    }
}

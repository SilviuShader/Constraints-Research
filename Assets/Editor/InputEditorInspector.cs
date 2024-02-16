using UnityEditor;
using UnityEngine;

namespace LevelsWFC
{
    [CustomEditor(typeof(InputEditor))]
    public class InputEditorInspector : Editor
    {
        private const float HandleSize = 0.04f;
        private const float PickSize   = 0.06f;

        private void OnSceneGUI()
        {
            var inputEditor = target as InputEditor;

            if (inputEditor == null)
                return;

            if (inputEditor.InputExample == null)
                return;

            var targetTransform = inputEditor.transform;

            Handles.color = Color.white;

            var h = inputEditor.EditLayer;
            if (h < inputEditor.InputExample.GridSize.y)
            {
                for (var w = 0; w < inputEditor.InputExample.GridSize.x; w++)
                {
                    for (var d = 0; d < inputEditor.InputExample.GridSize.z; d++)
                    {
                        var cellCenter = targetTransform.position +
                                         new Vector3(w + 0.5f, h + 0.5f, d + 0.5f) * InputEditor.CellSize;
                        Handles.DrawWireCube(cellCenter, Vector3.one * InputEditor.CellSize);

                        if (inputEditor.InputExample.Tileset == null)
                            continue;
                        if (inputEditor.InputExample.Tileset.Tiles.Length <= 0)
                            continue;

                        if (Handles.Button(cellCenter, Quaternion.identity, HandleSize, PickSize,
                                Handles.CubeHandleCap))
                        {
                            var newInfo = new InputExample.CellInfo
                            {
                                TileIndex = inputEditor.EditMode == InputEditor.EditModes.Paint ? inputEditor.PaintTileIndex : -1
                            };

                            inputEditor.InputExample.Cells[
                                InputExample.GridIndex(w, d, h, inputEditor.InputExample.GridSize)] = newInfo;

                            inputEditor.TreatDebugCell(new Vector3Int(w, h, d), newInfo);
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var inputEditor = target as InputEditor;
            
            if (inputEditor == null) 
                return;

            if (inputEditor.InputExample == null)
                return;

            EditorGUI.BeginChangeCheck();
            var previousGridSize = inputEditor.InputExample.GridSize;
            var gridSize = EditorGUILayout.Vector3IntField("Grid Size", previousGridSize);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(inputEditor.InputExample, "Resize Grid");
                EditorUtility.SetDirty(inputEditor.InputExample);
                Undo.RecordObject(inputEditor, "Resize Grid");
                EditorUtility.SetDirty(inputEditor);
                inputEditor.InputExample.GridSize = gridSize;
                inputEditor.OnValidate();
                inputEditor.ValidateGridResize(previousGridSize);
            }

            if (inputEditor.InputExample.GridSize.y >= 1)
            {
                EditorGUI.BeginChangeCheck();
                var editLayer = EditorGUILayout.IntSlider("Edit Layer", inputEditor.EditLayer, 0,
                    inputEditor.InputExample.GridSize.y - 1);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(inputEditor, "Change Edit Layer");
                    EditorUtility.SetDirty(inputEditor);
                    inputEditor.EditLayer = editLayer;
                    inputEditor.OnValidate();
                }
            }

            EditorGUI.BeginChangeCheck();
            var editMode = (InputEditor.EditModes)EditorGUILayout.EnumPopup("Edit Mode", inputEditor.EditMode);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(inputEditor, "Change Edit Type");
                EditorUtility.SetDirty(inputEditor);
                inputEditor.EditMode = editMode;
                inputEditor.OnValidate();
            }

            EditorGUI.BeginChangeCheck();
            var tileset = EditorGUILayout.ObjectField("Tileset", inputEditor.InputExample.Tileset, typeof(Tileset), false) as Tileset;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(inputEditor.InputExample, "Change Tileset");
                EditorUtility.SetDirty(inputEditor.InputExample);
                Undo.RecordObject(inputEditor, "Change Tileset");
                EditorUtility.SetDirty(inputEditor);
                inputEditor.InputExample.Tileset = tileset;
                inputEditor.OnValidate();

                inputEditor.DestroyDebugTiles();
                inputEditor.UpdateDebugTiles();
            }

            if (inputEditor.InputExample.Tileset == null)
                return;

            if (inputEditor.InputExample.Tileset.Tiles.Length <= 0)
                return;

            if (inputEditor.EditMode == InputEditor.EditModes.Paint)
            {
                var tileOptions = new string[inputEditor.InputExample.Tileset.Tiles.Length];
                for (var tileIndex = 0; tileIndex < tileOptions.Length; tileIndex++)
                    tileOptions[tileIndex] = inputEditor.InputExample.Tileset.Tiles[tileIndex].name;
                EditorGUI.BeginChangeCheck();
                var paintTileIndex = EditorGUILayout.Popup("Paint Tile", inputEditor.PaintTileIndex, tileOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(inputEditor, "Change Paint Tile");
                    EditorUtility.SetDirty(inputEditor);
                    inputEditor.PaintTileIndex = paintTileIndex;
                    inputEditor.OnValidate();
                }
            }
        }
    }
}
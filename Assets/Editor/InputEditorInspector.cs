using UnityEditor;
using UnityEngine;

namespace LevelsWFC
{
    [CustomEditor(typeof(InputEditor))]
    public class InputEditorInspector : Editor
    {
        private void OnSceneGUI()
        {
            var inputEditor = target as InputEditor;

            if (inputEditor == null)
                return;

            if (inputEditor.InputExample == null)
                return;

            Handles.color = Color.white;
            for (var w = 0; w < inputEditor.InputExample.GridSize.x; w++)
                for (var d = 0; d < inputEditor.InputExample.GridSize.z; d++)
                    for (var h = 0; h < inputEditor.InputExample.GridSize.y; h++)
                        Handles.DrawWireCube(new Vector3(w + 0.5f, h + 0.5f, d + 0.5f) * InputEditor.CellSize, Vector3.one * InputEditor.CellSize);
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

            EditorGUI.BeginChangeCheck();
            var tileset = EditorGUILayout.ObjectField("Tileset", inputEditor.InputExample.Tileset, typeof(Tileset), false) as Tileset;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(inputEditor.InputExample, "Resize Grid");
                EditorUtility.SetDirty(inputEditor.InputExample);
                Undo.RecordObject(inputEditor, "Resize Grid");
                EditorUtility.SetDirty(inputEditor);
                inputEditor.InputExample.Tileset = tileset;
                inputEditor.OnValidate();
            }
        }
    }
}
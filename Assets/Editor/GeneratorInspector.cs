using UnityEditor;
using UnityEngine;

namespace LevelsWFC
{
    [CustomEditor(typeof(Generator))]
    public class GeneratorInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var generator = target as Generator;

            if (generator == null)
                return;

            if (generator.InputExample == null)
                return;

            if (GUILayout.Button("Run"))
                generator.Run();

            if (GUILayout.Button("Clear"))
                generator.Clear();
        }
    }
}
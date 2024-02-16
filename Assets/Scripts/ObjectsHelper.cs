using UnityEngine;

namespace LevelsWFC
{
    public static class ObjectsHelper
    {
        public static void DestroyObjectEditor(GameObject obj)
        {
            UnityEditor.EditorApplication.delayCall += () =>
                Object.DestroyImmediate(obj);
        }
    }
}
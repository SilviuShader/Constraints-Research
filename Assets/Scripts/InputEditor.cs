using UnityEngine;

namespace LevelsWFC
{
    public class InputEditor : MonoBehaviour
    {
        public static float        CellSize     => 1.0f; 
        
        public        InputExample InputExample = null;

        public void OnValidate()
        {
            InputExample.GridSize.x = Mathf.Max(0, InputExample.GridSize.x);
            InputExample.GridSize.y = Mathf.Max(0, InputExample.GridSize.y);
            InputExample.GridSize.z = Mathf.Max(0, InputExample.GridSize.z);

            var targetCellsSize = InputExample.GridSize.x * InputExample.GridSize.y * InputExample.GridSize.z;

            if (InputExample.Cells == null)
            {
                InputExample.Cells = new InputExample.CellInfo[targetCellsSize];
                for (var i = 0; i < targetCellsSize; i++)
                    InputExample.Cells[i] = InputExample.CellInfo.Default;
            }
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
                            var oldIndex = w * (previousSize.z * previousSize.y) + d * previousSize.y + h;
                            if (oldIndex < InputExample.Cells.Length) 
                                cell = InputExample.Cells[oldIndex];
                        }
                        
                        newCells[w * (InputExample.GridSize.z * InputExample.GridSize.y) +
                                 d * InputExample.GridSize.y + h] = cell;
                    }
                }
            }

            InputExample.Cells = newCells;
        }
    }
}

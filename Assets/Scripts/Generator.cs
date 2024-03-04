using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor.Scripting.Python;

namespace LevelsWFC
{
    public class Generator : MonoBehaviour
    {
        public enum PatternType
        {
            NoOutBlock3,
            NeighPlus,
            Custom
        }

        [Serializable]
        public struct SubPattern
        {
            public Vector2Int[] Inputs;
            public Vector2Int[] Outputs;

            public string InputString()
            {
                if (Inputs.Length <= 0)
                    return "None";

                var pairsStrings = Inputs.Select(TuplesHelper.PairString).ToArray();
                return pairsStrings.Length > 1 ? "(" + string.Join(", ", pairsStrings) + ")" : "(" + pairsStrings[0] + ",)";
            }

            public string OutputString()
            {
                if (Outputs.Length <= 0)
                    return "None";

                var pairsStrings = Outputs.Select(TuplesHelper.PairString).ToArray();
                return pairsStrings.Length > 1 ? "(" + string.Join(", ", pairsStrings) + ")" : "(" + pairsStrings[0] + ",)";
            }

            public readonly Vector2Int Low()
            {
                var result = new Vector2Int(int.MaxValue, int.MaxValue);
                result = Inputs.Aggregate(result, Vector2Int.Min);
                return Outputs.Aggregate(result, Vector2Int.Min);
            }

            public readonly Vector2Int High()
            {
                var result = new Vector2Int(int.MinValue, int.MinValue);
                result = Inputs.Aggregate(result, Vector2Int.Max);
                return Outputs.Aggregate(result, Vector2Int.Max);
            }
        }

        [Serializable]
        public struct Pattern
        {
            public SubPattern[] SubPatterns;

            public readonly Vector2Int Low()
            {
                var result = new Vector2Int(int.MaxValue, int.MaxValue);
                result = SubPatterns.Select(x => x.Low()).Aggregate(result, Vector2Int.Min);
                return result;
            }

            public readonly Vector2Int High()
            {
                var result = new Vector2Int(int.MinValue, int.MinValue);
                result = SubPatterns.Select(x => x.High()).Aggregate(result, Vector2Int.Max);
                return result;
            }

            public static Pattern Block3 => new()
            {
                SubPatterns = new SubPattern[]
                {
                    new()
                    {
                        Inputs = new[]
                        {
                            Vector2Int.zero,
                            new Vector2Int(0, 1),
                            new Vector2Int(0, 2),
                            new Vector2Int(1, 0),
                            new Vector2Int(1, 1),
                            new Vector2Int(1, 2),
                            new Vector2Int(2, 0),
                            new Vector2Int(2, 1),
                            new Vector2Int(2, 2)
                        },
                        Outputs = Array.Empty<Vector2Int>()
                    }
                }
            };

            public static Pattern NeighPlus => new()
            {
                SubPatterns = new SubPattern[]
                {
                    new()
                    {
                        Inputs = new[]
                        {
                            Vector2Int.zero
                        },
                        Outputs = new[]
                        {
                            new Vector2Int(0, 1)
                        }
                    },
                    new()
                    {
                        Inputs = new[]
                        {
                            Vector2Int.zero
                        },
                        Outputs = new[]
                        {
                            new Vector2Int(1, 0)
                        }
                    },
                    new()
                    {
                        Inputs = new[]
                        {
                            Vector2Int.zero
                        },
                        Outputs = new[]
                        {
                            new Vector2Int(0, -1)
                        }
                    },
                    new()
                    {
                        Inputs = new[]
                        {
                            Vector2Int.zero
                        },
                        Outputs = new[]
                        {
                            new Vector2Int(-1, 0)
                        }
                    }
                }
            };
        }

        public InputExample InputExample  = null;
        public Vector3Int   Size          = new(15, 0, 15);
        public PatternType  PatternCells  = PatternType.NoOutBlock3;
        
        public Pattern      CustomPattern = Pattern.Block3;

        public void Run()
        {
            if (InputExample == null)
                return;

            if (!InputExample.Valid)
            {
                Debug.LogError("The Input Example must be full of tiles.");
                return;
            }

            dynamic obj = new ExpandoObject();
            (obj as IDictionary<string, object>).Add("py/object", "util_common.SchemeInfo");
            dynamic tileset = new ExpandoObject();

            var tileIds = new Dictionary<int, object>();

            var tilesSet = new HashSet<int>();
            foreach (var cell in InputExample.Cells)
                if (!tilesSet.Contains(cell.TileIndex))
                    tilesSet.Add(cell.TileIndex);
            var tilesArray = tilesSet.OrderBy(x => x).ToArray();
            foreach (var tile in tilesArray)
                tileIds[tile] = null;

            (tileset as IDictionary<string, object>).Add("py/object", "util_common.TileSetInfo");
            tileset.tile_ids = tileIds;
            var tileToText = new Dictionary<int, char>();
            foreach (var tile in tilesArray)
                tileToText[tile] = TileCharEncoding(tile);
            tileset.tile_to_text = tileToText;

            var tileToImage = new Dictionary<int, dynamic>();

            foreach (var tile in tilesArray)
            {
                var list = new List<dynamic>
                {
                    new object(),
                    "RGBA",
                    new Dictionary<string, int[]> { {"py/tuple", new[] { 1, 1 }}},
                    null,
                    new Dictionary<string, string>()
                    {
                        {"py/b64", TileColorEncoding(tile)}
                    }
                };

                var pilImage = new ExpandoObject();
                ((IDictionary<string, object>)pilImage).Add("py/object", "PIL.Image.Image");
                ((IDictionary<string, object>)pilImage).Add("py/state", list);
                tileToImage[tile] = pilImage;
            }

            tileset.tile_to_image = tileToImage;
            tileset.tile_image_size = 1;

            obj.tileset = tileset;

            // TODO: Handle tags
            var gameToTagToTiles = new Dictionary<string, Dictionary<string, Dictionary<int, object>>>
            {
                [","] = new (){{",", tileIds}}
            };

            obj.game_to_tag_to_tiles = gameToTagToTiles;

            dynamic countInfo = new ExpandoObject();
            ((IDictionary<string, object>)countInfo).Add("py/object", "util_common.SchemeCountInfo");
            // TODO: Don't know what this is
            countInfo.divs_size = new[] { 1, 1 };

            // TODO: Handle tags
            // TODO: Handle different patterns.
            var divsToGameToTagToTileCount =
                new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<int, float>>>>();
            var origin = new Dictionary<string, Dictionary<string, Dictionary<int, float>>>();

            var tilesCounts = new FlexibleDictionary<int, int>();
            foreach (var cell in InputExample.Cells)
            {
                if (!tilesCounts.ContainsKey(cell.TileIndex))
                    tilesCounts[cell.TileIndex] = 0;
                tilesCounts[cell.TileIndex]++;
            }
            var tilesFrequencies = new Dictionary<int, float>();
            foreach (var keyVal in tilesCounts)
                tilesFrequencies[keyVal.Key] = (float)keyVal.Value / InputExample.Cells.Length;
            
            origin[","] = new Dictionary<string, Dictionary<int, float>> { { ",", tilesFrequencies } };

            // TODO: Hard-coded pattern
            divsToGameToTagToTileCount["(0, 0)"] = origin;
            countInfo.divs_to_game_to_tag_to_tile_count = divsToGameToTagToTileCount;
            obj.count_info = countInfo;

            // TODO: Handle tags
            // TODO: Handle different patterns
            var pattern = GetPattern();
            dynamic patternInfo = new ExpandoObject();

            dynamic gameToPatterns = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>>>();
            var topInputDictionary =
                new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>>();
            var inputDictionary = new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();

            foreach (var subPattern in pattern.SubPatterns)
            {
                var patterns = ExtractPatterns(subPattern);

                var patternInputKey = subPattern.InputString();
                if (!topInputDictionary.ContainsKey(patternInputKey))
                    topInputDictionary[patternInputKey] =
                        new Dictionary<string, Dictionary<string, Dictionary<string, dynamic>>>();

                foreach (var inputKey in patterns.Keys)
                {
                    var patternsDictionary = new Dictionary<string, dynamic>();
                    foreach (var p in patterns[inputKey])
                    {
                        patternsDictionary[PatternToString(p)] = new Dictionary<string, Dictionary<string, object>>
                        {
                            { "null", new Dictionary<string, object> { { "null", null } } }
                        };
                    }

                    var outputs = new Dictionary<string, Dictionary<string, dynamic>>()
                    {
                        { subPattern.OutputString(), patternsDictionary }
                    };
                    inputDictionary[PatternToString(inputKey)] = outputs;
                }

                foreach (var elem in inputDictionary)
                {
                    if (!topInputDictionary[patternInputKey].ContainsKey(elem.Key))
                        topInputDictionary[patternInputKey][elem.Key] = elem.Value;
                    else
                        foreach (var elem2 in elem.Value)
                            if (!topInputDictionary[patternInputKey][elem.Key].ContainsKey(elem2.Key))
                                topInputDictionary[patternInputKey][elem.Key][elem2.Key] = elem2.Value;
                }
            }

            gameToPatterns[","] = topInputDictionary;

            ((IDictionary<string, object>)patternInfo).Add("py/object", "util_common.SchemePatternInfo");
            patternInfo.game_to_patterns = gameToPatterns;
            

            var low = pattern.Low();
            var high = pattern.High();

            patternInfo.stride_rows = 1;
            patternInfo.stride_cols = 1;
            patternInfo.dr_lo = low.y;
            patternInfo.dr_hi = high.y;
            patternInfo.dc_lo = low.x;
            patternInfo.dc_hi = high.x;
            obj.pattern_info = patternInfo;
            obj.output_size = TuplesHelper.PairString(Size);

            var json = JsonConvert.SerializeObject(obj);
            File.WriteAllText(Application.dataPath + "/Python/scheme.json", json);
            PythonRunner.EnsureInitialized();
            try
            {
                PythonRunner.RunFile($"{Application.dataPath}/Python/scheme2output.py", "__main__");
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            Clear();

            var generatedLevel = File.ReadAllText(Application.dataPath + "/Python/output.lvl");
            var lines = generatedLevel.Split("\n").Where(x => x.Length != 0);
            var w = 0;
            foreach (var line in lines)
            {
                var d = 0;
                foreach (var tile in line.Select(TileCharDecoding))
                {
                    if (tile < 0 || tile >= InputExample.Tileset.Tiles.Length) 
                        continue;

                    SpawnTile(new Vector3Int(w, 0, d), tile);
                    d++;
                }
                w++;
            }
        }

        public void Clear()
        {
            foreach (Transform child in transform)
                ObjectsHelper.DestroyObjectEditor(child.gameObject);
        }

        private Pattern GetPattern()
        {
            switch (PatternCells)
            {
                case PatternType.NoOutBlock3:
                    return Pattern.Block3;
                case PatternType.NeighPlus:
                    return Pattern.NeighPlus;
                case PatternType.Custom:
                    return CustomPattern;
            }

            return CustomPattern;
        }

        private string TileColorEncoding(int tile) => 
            Convert.ToBase64String(BitConverter.GetBytes(tile));

        private char TileCharEncoding(int tile) => (char)(tile + 32);

        private int TileCharDecoding(char tile) => tile - 32;

        private Dictionary<int[], HashSet<int[]>> ExtractPatterns(in SubPattern pattern)
        {
            var patterns = new Dictionary<int[], HashSet<int[]>>(new SequenceComparer<int>());

            var bottomLeft = pattern.Low();
            var topRight   = pattern.High();

            var patternSize = topRight - bottomLeft;

            for (var w = -patternSize.x; w < InputExample.GridSize.x + patternSize.x; w++)
            {
                for (var d = -patternSize.y; d < InputExample.GridSize.z + patternSize.y; d++)
                {
                    var currentInputPatternTiles  = new int[pattern.Inputs.Length];
                    var currentOutputPatternTiles = new int[pattern.Outputs.Length];
                    var index = 0;

                    foreach (var input in pattern.Inputs)
                    {
                        var currentPos = input - bottomLeft + new Vector2Int(w, d);
                        currentInputPatternTiles[index++] = InputExample.ValidGridIndex(currentPos.x, currentPos.y, 0, InputExample.GridSize) ? InputExample.Cells[InputExample.GridIndex(currentPos.x, currentPos.y, 0, InputExample.GridSize)].TileIndex : -1;
                    }

                    index = 0;
                    foreach (var output in pattern.Outputs)
                    {
                        var currentPos = output - bottomLeft + new Vector2Int(w, d);
                        currentOutputPatternTiles[index++] = InputExample.ValidGridIndex(currentPos.x, currentPos.y, 0, InputExample.GridSize) ? InputExample.Cells[InputExample.GridIndex(currentPos.x, currentPos.y, 0, InputExample.GridSize)].TileIndex : -1;
                    }

                    //if (currentInputPatternTiles.All(x => x == -1) && currentOutputPatternTiles.All(x => x == -1))
                    //    continue;

                    if (!patterns.ContainsKey(currentInputPatternTiles))
                        patterns[currentInputPatternTiles] = new HashSet<int[]>(new SequenceComparer<int>());
                    
                    if (!patterns[currentInputPatternTiles].Contains(currentOutputPatternTiles))
                        patterns[currentInputPatternTiles].Add(currentOutputPatternTiles);
                }
            }

            return patterns;
        }

        private string PatternToString(int[] patternTiles)
        {
            if (patternTiles.Length <= 0)
                return "None";

            return patternTiles.Length > 1 ? "(" + string.Join(", ", patternTiles) + ")" : "(" + patternTiles[0] + ",)";
        }

        private void SpawnTile(Vector3Int position, int tileIndex) =>
            SpawnHelper.SpawnTile(position, InputExample.Tileset.Tiles[tileIndex], transform);
    }
}
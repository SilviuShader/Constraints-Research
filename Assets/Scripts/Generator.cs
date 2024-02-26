using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;


namespace LevelsWFC
{
    public class Generator : MonoBehaviour
    {
        public InputExample InputExample = null;

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
            dynamic tileset = new ExpandoObject();

            var tileIds = new Dictionary<int, object>();

            var tilesSet = new HashSet<int>();
            foreach (var cell in InputExample.Cells)
                if (!tilesSet.Contains(cell.TileIndex))
                    tilesSet.Add(cell.TileIndex);
            var tilesArray = tilesSet.OrderBy(x => x).ToArray();
            foreach (var tile in tilesArray)
                tileIds[tile] = null;
            
            tileset.tile_ids = tileIds;
            tileset.tile_to_text = null;
            var tileToImage = new Dictionary<int, List<dynamic>>();

            foreach (var tile in tilesArray)
            {
                var list = new List<dynamic>
                {
                    new object(),
                    "RGBA",
                    new[] { 1, 1 },
                    null,
                    new Dictionary<string, string>()
                    {
                        {"py/b64", TileColorEncoding(tile)}
                    }
                };
                tileToImage[tile] = list;
            }

            tileset.tile_to_image = tileToImage;
            tileset.tile_image_size = 1;

            var gameToTagToTiles = new Dictionary<string, Dictionary<string, Dictionary<int, object>>>
            {
                [","] = new (){{",", tileIds}}
            };

            tileset.game_to_tag_to_tiles = gameToTagToTiles;

            dynamic countInfo = new ExpandoObject();

            countInfo.divs_size = new[] { 1, 1 };

            tileset.count_info = countInfo;

            obj.tileset = tileset;

            var json = JsonConvert.SerializeObject(obj);
            int a;
            a = 0;
        }

        private string TileColorEncoding(int tile) => Convert.ToBase64String(BitConverter.GetBytes(tile));
    }
}
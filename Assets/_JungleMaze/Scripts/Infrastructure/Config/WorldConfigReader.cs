using System.IO;
using JungleMaze.World;
using JungleMaze.World.Inventory;
using Newtonsoft.Json;
namespace JungleMaze.Infrastructure.Config
{
    public sealed class WorldConfigData
    {
        public string Version="world-prototype-1";
        public uint Seed=20261028;
        public string LayoutMode="Authored";
        public MapLayout Layout;
        public MapLayoutConfig Map=new MapLayoutConfig();
        public DestructibleConfig Destructibles=new DestructibleConfig();
        public HazardConfig Hazards=new HazardConfig();
        public static WorldConfigData Load(string streamingAssets)
        {
            var path=Path.Combine(streamingAssets,"JungleMaze/runtime/world_settings.json");
            var data=JsonConvert.DeserializeObject<WorldConfigData>(File.ReadAllText(path));
            if(data==null||data.Map==null||data.Destructibles==null||data.Hazards==null)throw new InvalidDataException("Missing world configuration sections.");
            data.Map.Version=data.Version;data.Map.Validate();data.Destructibles.Validate();data.Hazards.Validate();
            if(data.LayoutMode=="Authored")
            {
                // World offline dùng map offline riêng nếu có (map lớn của Level Editor), không thì map_demo của server.
                var offline=Path.Combine(streamingAssets,"JungleMaze/runtime",JungleMaze.App.Ports.RuntimeDataFiles.OfflineMap);
                var mapPath=File.Exists(offline)?offline:Path.Combine(streamingAssets,"JungleMaze/runtime",JungleMaze.App.Ports.RuntimeDataFiles.ServerMap);
                var map=JsonConvert.DeserializeObject<AuthoredMapDocument>(File.ReadAllText(mapPath));
                if(map==null)throw new InvalidDataException("Missing authored map.");
                data.Layout=map.Build(data.Version);
            }
            else if(data.LayoutMode!="Prototype")throw new InvalidDataException("Unknown world layout mode.");
            return data;
        }
        public static ItemCatalogDocument LoadItems(string streamingAssets)
        {
            var path=Path.Combine(streamingAssets,"JungleMaze/runtime/items_catalog.json");
            var document=JsonConvert.DeserializeObject<ItemCatalogDocument>(File.ReadAllText(path));
            if(document==null)throw new InvalidDataException("Missing item catalog.");
            document.Build();return document;
        }
    }
}

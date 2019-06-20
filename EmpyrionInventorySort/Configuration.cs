using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace EmpyrionInventorySort
{
    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public string ChatCommandPrefix { get; set; } = "/\\";
        public ConcurrentDictionary<string, PlayerSortings> PlayerSortings { get; set; } = new ConcurrentDictionary<string, PlayerSortings>();
    }

    public class PlayerSortings
    {
        public Dictionary<int, int> Bag     { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> Toolbar { get; set; } = new Dictionary<int, int>();
    }
}
using EmpyrionNetAPIDefinitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace EmpyrionInventorySort
{
    public class Configuration
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevel LogLevel { get; set; } = LogLevel.Message;
        public string ChatCommandPrefix { get; set; } = "/\\";
    }

    public class PlayerSortings
    {
        public string PlayerName { get; set; }
        public int LastUsedSlot { get; set; }
        public SortingSlot[] SortingSlot { get; set; } = new SortingSlot[10];
    }

    public class SortingSlot
    {
        public List<ItemSlot> Bag     { get; set; }
        public List<ItemSlot> Toolbar { get; set; }
    }

    public class ItemSlot
    {
        public int ItemId { get; set; }
        public int SlotPos { get; set; }
    }
}
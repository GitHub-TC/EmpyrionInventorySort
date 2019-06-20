using Eleon.Modding;
using EmpyrionNetAPIAccess;
using EmpyrionNetAPITools;
using EmpyrionNetAPIDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace EmpyrionInventorySort
{
    public class InventorySort : EmpyrionModBase
    {
        public ModGameAPI GameAPI { get; private set; }
        public ConfigurationManager<Configuration> Configuration { get; set; }

        public InventorySort()
        {
            EmpyrionConfiguration.ModName = "EmpyrionInventorySort";
        }

        public override void Initialize(ModGameAPI dediAPI)
        {
            GameAPI = dediAPI;

            log($"**HandleEmpyrionTeleporter loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeConfiguration();
            LogLevel = Configuration.Current.LogLevel;
            ChatCommandManager.CommandPrefix = Configuration.Current.ChatCommandPrefix;

            ChatCommands.Add(new ChatCommand(@"s",         (I, A) => SortInventory(I), "Execute sorting"));
            ChatCommands.Add(new ChatCommand(@"sort",      (I, A) => SortInventory(I), "Execute sorting"));
            ChatCommands.Add(new ChatCommand(@"sort set",  (I, A) => SortSet(I), "Saves the current sorting"));
            ChatCommands.Add(new ChatCommand(@"sort help", (I, A) => DisplayHelp(I.playerId, ""), "Display help"));
        }

        private async Task SortSet(ChatInfo chatInfo)
        {
            var P = await Request_Player_Info(chatInfo.playerId.ToId());

            var sort = new PlayerSortings() {
                Bag     = P.bag    .ToDictionary(I => I.id, I => (int)I.slotIdx),
                Toolbar = P.toolbar.ToDictionary(I => I.id, I => (int)I.slotIdx),
            };
            Configuration.Current.PlayerSortings.AddOrUpdate(P.steamId, sort, (S, O) => sort);
            Configuration.Save();

            MessagePlayer(chatInfo.playerId, "Inventorysorting saved");
        }

        private async Task SortInventory(ChatInfo chatInfo)
        {
            var P = await Request_Player_Info(chatInfo.playerId.ToId());
            if (!Configuration.Current.PlayerSortings.TryGetValue(P.steamId, out var sort))
            {
                MessagePlayer(chatInfo.playerId, $"Sorry no sorting saved. Please use '{ChatCommandManager.CommandPrefix?.FirstOrDefault()}sort set' before.");
                return;
            }

            var allitems = new List<ItemStack>();
            if (P.bag     != null) allitems.AddRange(P.bag);
            if (P.toolbar != null) allitems.AddRange(P.toolbar);

            if (allitems.Count == 0) {
                MessagePlayer(chatInfo.playerId, "Nothing to sort.");
                return;
            }

            var bag     = GetFromSorting(sort.Bag,     allitems, 40);
            var toolbar = GetFromSorting(sort.Toolbar, allitems, 9);

            AddUnknownItems(allitems, bag);
            AddUnknownItems(allitems, toolbar);

            await Request_Player_SetInventory(new Inventory(
                chatInfo.playerId, 
                toolbar.ToArray(), 
                bag    .ToArray()
            ));

            MessagePlayer(chatInfo.playerId, "Inventory sorted");
        }

        private void AddUnknownItems(List<ItemStack> allItems, ItemStack[] sortItems)
        {
            for (int i = 0; i < sortItems.Length && allItems.Count > 0; i++)
            {
                var currentItem = sortItems[i];
                if (currentItem.id == 0)
                {
                    sortItems[i] = allItems.First();
                    allItems.RemoveAt(0);
                }
            }
        }

        private static ItemStack[] GetFromSorting(Dictionary<int, int> sort, List<ItemStack> allitems, int maxSlots)
        {
            var result = Enumerable.Range(0, maxSlots).Select(I => new ItemStack() { slotIdx = (byte)I }).ToArray();

            for (int i = 0; i < allitems.Count; i++)
            {
                var currentItem = allitems[i];
                if(sort.TryGetValue(currentItem.id, out var idxPos))
                {
                    currentItem.slotIdx = (byte)idxPos;
                    result[idxPos] = currentItem;
                    allitems.RemoveAt(i--);
                }
            }

            return result;
        }

        private void InitializeConfiguration()
        {
            Configuration = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json")
            };

            Configuration.Load();
            Configuration.Save();
        }
    }
}

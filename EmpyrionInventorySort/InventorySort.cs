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

            Log($"**HandleEmpyrionTeleporter loaded: {string.Join(" ", Environment.GetCommandLineArgs())}", LogLevel.Message);

            InitializeConfiguration();
            LogLevel = Configuration.Current.LogLevel;
            ChatCommandManager.CommandPrefix = Configuration.Current.ChatCommandPrefix;
            var chatChar = ChatCommandManager.CommandPrefix?.FirstOrDefault() ?? '/';

            ChatCommands.Add(new ChatCommand(@"s",                          (I, A) => SortInventory(I, A),  $"Execute sorting, latest slot from '{chatChar}sort set' or '{chatChar}sort' command"));
            ChatCommands.Add(new ChatCommand(@"sort (?<number>\d)",         (I, A) => SortInventory(I, A),  $"Execute sorting (0..9). Set the slot for '{chatChar}s'"));
            ChatCommands.Add(new ChatCommand(@"sort set (?<number>\d)",     (I, A) => SortSet(I, A),        $"Saves the current sorting slot (0..9). Set the slot for '{chatChar}s'"));
            ChatCommands.Add(new ChatCommand(@"sort help",                  (I, A) => DisplayHelp(I.playerId, ""), "Display help"));
        }

        private async Task SortSet(ChatInfo chatInfo, Dictionary<string, string> args)
        {
            var P = await Request_Player_Info(chatInfo.playerId.ToId());

            var currentPlayerSort = GetPlayerSorting(P);

            Log($"{currentPlayerSort} -> {currentPlayerSort.Current} -> {currentPlayerSort.ConfigFilename}", LogLevel.Error);

            int sortSlot = currentPlayerSort.Current.LastUsedSlot;
            if (args.TryGetValue("number", out string numberArgs)) int.TryParse(numberArgs, out sortSlot);
            currentPlayerSort.Current.LastUsedSlot = sortSlot;

            currentPlayerSort.Current.SortingSlot[sortSlot] = new SortingSlot() {
                Bag     = P.bag?    .Select(I => new ItemSlot() { ItemId = I.id, SlotPos = (int)I.slotIdx }).ToList() ?? new List<ItemSlot>(),
                Toolbar = P.toolbar?.Select(I => new ItemSlot() { ItemId = I.id, SlotPos = (int)I.slotIdx }).ToList() ?? new List<ItemSlot>(),
            };

            currentPlayerSort.Current.PlayerName = P.playerName;
            currentPlayerSort.Save();

            MessagePlayer(chatInfo.playerId, "Inventorysorting saved");
        }

        public ConfigurationManager<PlayerSortings> GetPlayerSorting(PlayerInfo player)
        {
            var sort = new ConfigurationManager<PlayerSortings>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Sortings", player.steamId + ".json")
            };
            sort.Load();
            return sort;
        }

        private async Task SortInventory(ChatInfo chatInfo, Dictionary<string, string> args)
        {
            var P = await Request_Player_Info(chatInfo.playerId.ToId());

            var currentPlayerSort = GetPlayerSorting(P);

            int sortSlot = currentPlayerSort.Current.LastUsedSlot;
            if (args.TryGetValue("number", out string numberArgs)) int.TryParse(numberArgs, out sortSlot);

            if (currentPlayerSort.Current.SortingSlot[sortSlot] == null)
            {
                MessagePlayer(chatInfo.playerId, $"Sorry no sorting saved. Please use '{ChatCommandManager.CommandPrefix?.FirstOrDefault()}sort set {sortSlot}' before.");
                return;
            }

            var allitems = new List<ItemStack>();
            if (P.bag     != null) allitems.AddRange(P.bag);
            if (P.toolbar != null) allitems.AddRange(P.toolbar);

            if (allitems.Count == 0) {
                MessagePlayer(chatInfo.playerId, "Nothing to sort.");
                return;
            }

            var bag     = GetFromSorting(currentPlayerSort.Current.SortingSlot[sortSlot].Bag,     allitems, 40);
            var toolbar = GetFromSorting(currentPlayerSort.Current.SortingSlot[sortSlot].Toolbar, allitems, 9);

            AddUnknownItems(allitems, bag);
            AddUnknownItems(allitems, toolbar);

            await Request_Player_SetInventory(new Inventory(
                chatInfo.playerId, 
                toolbar.ToArray(), 
                bag    .ToArray()
            ));

            if (currentPlayerSort.Current.LastUsedSlot != sortSlot)
            {
                currentPlayerSort.Current.PlayerName = P.playerName;
                currentPlayerSort.Current.LastUsedSlot = sortSlot;
                currentPlayerSort.Save();
            }

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

        private static ItemStack[] GetFromSorting(List<ItemSlot> sort, List<ItemStack> allitems, int maxSlots)
        {
            var result   = Enumerable.Range(0, maxSlots).Select(I => new ItemStack() { slotIdx = (byte)I }).ToArray();
            var sortList = new List<ItemSlot>(sort);

            for (int i = 0; i < allitems.Count; i++)
            {
                var currentItem = allitems[i];
                var found = sortList.FirstOrDefault(I => I.ItemId == currentItem.id);
                if (found != null)
                {
                    sortList.Remove(found);
                    currentItem.slotIdx = (byte)found.SlotPos;
                    result[found.SlotPos] = currentItem;
                    allitems.RemoveAt(i--);
                }
            }

            return result;
        }

        private void InitializeConfiguration()
        {
            ConfigurationManager<Configuration>.Log = Log;
            Configuration = new ConfigurationManager<Configuration>()
            {
                ConfigFilename = Path.Combine(EmpyrionConfiguration.SaveGameModPath, "Configuration.json")
            };

            Configuration.Load();
            Configuration.Save();
        }
    }
}

using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System;

namespace TagRides.Shared.Game
{
    /// <summary>
    /// Inventory of game items
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class GameInventory
    {
        public IEnumerable<GameItem> Items => items.Values;

        /// <summary>
        /// Adds an item if non of the same name exist. If one of the same name does exist, false is returned.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool AddItem(GameItem item)
        {
            return items.TryAdd(item.Name, item);
        }

        /// <summary>
        /// Removes the item with name <paramref name="name"/> if it exists. Returns false otherwise
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveItem(string name)
        {
            return items.TryRemove(name, out GameItem i);
        }

        /// <summary>
        /// Get an item by it's name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public GameItem GetItem(string name)
        {
            if (!items.TryGetValue(name, out GameItem item))
                return null;
            return item;
        }

        /// <summary>
        /// Adds <paramref name="item"/> to the inventory.
        /// If an item of the same name already exists, <paramref name="item"/> will
        /// replace the old value, with the counts being merged.
        /// </summary>
        /// <param name="item"></param>
        public void AddOrUpdate(GameItem item)
        {
            items.AddOrUpdate(item.Name, item,
                (key, value) =>
                {
                    if (item.MaxCount == -1)
                        item.Count += value.Count;
                    else
                        item.Count = Math.Min(item.MaxCount, value.Count + item.Count);
                    return item;
                });
        }

        /// <summary>
        /// Reduce the stack by one
        /// </summary>
        /// <param name="name"></param>
        /// <returns>The specified item if it exists, and was successfully reduced. Null otherwise</returns>
        public GameItem TakeOne(string name, bool clearIfEmpty = true)
        {
            GameItem item = GetItem(name);
            if (item == null || item.Count < 1)
                return null;

            item.Count -= 1;

            if (item.Count == 0 && clearIfEmpty)
                RemoveItem(name);

            return item;
        }

        [JsonProperty]
        readonly ConcurrentDictionary<string, GameItem> items = new ConcurrentDictionary<string, GameItem>();
    }
}

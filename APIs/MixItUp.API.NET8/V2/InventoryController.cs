using Microsoft.AspNetCore.Mvc;
using MixItUp.API.V2.Models;
using MixItUp.Base;
using MixItUp.Base.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MixItUp.API.NET8.V2
{
    [ApiController]
    [Route("api/v2/inventory")]
    public class InventoryController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<GetInventoryResponse>> GetInventories()
        {
            var inventories = new List<GetInventoryResponse>();
            foreach (var inventory in ChannelSession.Settings.Inventory)
            {
                var items = new List<GetInventoryItemResponse>();
                foreach (var item in inventory.Value.Items)
                {
                    items.Add(new GetInventoryItemResponse
                    {
                        ID = item.Value.ID,
                        Name = item.Value.Name
                    });
                }
                inventories.Add(new GetInventoryResponse
                {
                    ID = inventory.Value.ID,
                    Name = inventory.Value.Name,
                    Items = items
                });
            }
            return Ok(inventories);
        }

        [HttpGet("{inventoryId:guid}/{userId:guid}")]
        public async Task<ActionResult<List<GetInventoryItemAmountResponse>>> GetInventoryItemAmountsForUser(Guid inventoryId, Guid userId)
        {
            if (!ChannelSession.Settings.Inventory.TryGetValue(inventoryId, out var inventory) || inventory == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            var itemAmounts = new List<GetInventoryItemAmountResponse>();
            foreach (var item in inventory.Items)
            {
                int amount = inventory.GetAmount(user, item.Value);
                if (amount > 0)
                {
                    itemAmounts.Add(new GetInventoryItemAmountResponse()
                    {
                        ID = item.Value.ID,
                        Name = item.Value.Name,
                        Amount = amount
                    });
                }
            }
            return Ok(itemAmounts);
        }

        [HttpGet("{inventoryId:guid}/{itemId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> GetInventoryItemAmountForUser(Guid inventoryId, Guid itemId, Guid userId)
        {
            if (!ChannelSession.Settings.Inventory.TryGetValue(inventoryId, out var inventory) || inventory == null)
            {
                return NotFound();
            }
            var item = inventory.GetItem(itemId);
            if (item == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            return Ok(inventory.GetAmount(user, item));
        }

        [HttpPatch("{inventoryId:guid}/{itemId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> UpdateInventoryItemAmountForUser(Guid inventoryId, Guid itemId, Guid userId, [FromBody] UpdateInventoryAmount updateAmount)
        {
            if (!ChannelSession.Settings.Inventory.TryGetValue(inventoryId, out var inventory) || inventory == null)
            {
                return NotFound();
            }
            var item = inventory.GetItem(itemId);
            if (item == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            if (updateAmount.Amount > 0)
            {
                inventory.AddAmount(user, item, updateAmount.Amount);
            }
            else if (updateAmount.Amount < 0)
            {
                inventory.SubtractAmount(user, item, -1 * updateAmount.Amount);
            }
            return Ok(inventory.GetAmount(user, item));
        }

        [HttpPut("{inventoryId:guid}/{itemId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> SetInventoryItemAmountForUser(Guid inventoryId, Guid itemId, Guid userId, [FromBody] UpdateInventoryAmount updateAmount)
        {
            if (!ChannelSession.Settings.Inventory.TryGetValue(inventoryId, out var inventory) || inventory == null)
            {
                return NotFound();
            }
            var item = inventory.GetItem(itemId);
            if (item == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            inventory.SetAmount(user, item, updateAmount.Amount);
            return Ok(inventory.GetAmount(user, item));
        }
    }
}

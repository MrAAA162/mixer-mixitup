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
    [Route("api/v2/currency")]
    public class CurrencyController : ControllerBase
    {
        [HttpGet]
        public ActionResult<List<GetCurrencyResponse>> GetCurrencies()
        {
            var currencies = new List<GetCurrencyResponse>();
            foreach (var currency in ChannelSession.Settings.Currency)
            {
                currencies.Add(new GetCurrencyResponse
                {
                    ID = currency.Value.ID,
                    Name = currency.Value.Name
                });
            }
            return Ok(currencies);
        }

        [HttpGet("{currencyId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> GetCurrencyAmountForUser(Guid currencyId, Guid userId)
        {
            if (!ChannelSession.Settings.Currency.TryGetValue(currencyId, out var currency) || currency == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            return Ok(currency.GetAmount(user));
        }

        [HttpPatch("{currencyId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> UpdateCurrencyAmountForUser(Guid currencyId, Guid userId, [FromBody] UpdateCurrencyAmount updateAmount)
        {
            if (!ChannelSession.Settings.Currency.TryGetValue(currencyId, out var currency) || currency == null)
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
                currency.AddAmount(user, updateAmount.Amount);
            }
            else if (updateAmount.Amount < 0)
            {
                currency.SubtractAmount(user, -1 * updateAmount.Amount);
            }
            return Ok(currency.GetAmount(user));
        }

        [HttpPut("{currencyId:guid}/{userId:guid}")]
        public async Task<ActionResult<int>> SetCurrencyAmountForUser(Guid currencyId, Guid userId, [FromBody] UpdateCurrencyAmount updateAmount)
        {
            if (!ChannelSession.Settings.Currency.TryGetValue(currencyId, out var currency) || currency == null)
            {
                return NotFound();
            }
            await ServiceManager.Get<UserService>().LoadAllUserData();
            if (!ChannelSession.Settings.Users.TryGetValue(userId, out var user) || user == null)
            {
                return NotFound();
            }
            currency.SetAmount(user, updateAmount.Amount);
            return Ok(currency.GetAmount(user));
        }
    }
}

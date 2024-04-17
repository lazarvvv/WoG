using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Security.Claims;
using WoG.Characters.Services.Api.Data;
using WoG.Characters.Services.Api.Models;
using WoG.Characters.Services.Api.Models.Dtos;
using WoG.Characters.Services.Api.Services;

namespace WoG.Characters.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<ItemApiController> logger;

        private readonly IMapper mapper;
        private readonly IDatabase redisDatabase;

        public ItemApiController(ApplicationDbContext dbContext, IConnectionMultiplexer redisConnectionMultiplexer,
            ILogger<ItemApiController> Logger, IMapper mapper)
        {
            this.redisDatabase = redisConnectionMultiplexer.GetDatabase();

            this.dbContext = dbContext;
            this.logger = Logger;
            this.mapper = mapper;
        }

        [HttpGet, Authorize(Roles = "GM")]
        public ObjectResult Get()
        {
            try
            {
                var result = dbContext.Items.Include(x => x.BaseItem).ToList();
                var response = mapper.Map<IEnumerable<ItemDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Get :: Error when trying to get Item data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpPost, Authorize(Roles = "GM")]
        [Route("Grant")]
        public ObjectResult Grant([FromBody] ItemDto itemDto)
        {
            try
            {
                var result = mapper.Map<Item>(itemDto);

                this.dbContext.Add(result);
                this.dbContext.SaveChanges();
                
                var response = mapper.Map<IEnumerable<ItemDto>>(result);
                
                redisDatabase.KeyDelete($"{nameof(CharacterDto)}:{itemDto.OwnerId}");

                logger.LogInformation("Grant :: Character with Id = {characterId} granted item with Id = {itemId}", result.OwnerId, result.Id);

                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Get :: Error when trying to get Item data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpPost]
        [Route("Gift"), Authorize]
        public async Task<ObjectResult> Gift([FromBody] ItemGiftDto itemGiftDto)
        {
            try
            {
                var character = await dbContext.Characters.FirstAsync(x => x.Id == itemGiftDto.GiftFrom);
                var idFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User not authenticated.");

                if (!idFromToken.Equals(itemGiftDto.GiftFrom.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return this.StatusCode(StatusCodes.Status403Forbidden, $"User with id = {itemGiftDto.GiftFrom} not authorized to view this resource");
                }

                var item = dbContext.Items.First(x => x.Id == itemGiftDto.ItemId && x.OwnerId == itemGiftDto.GiftFrom);
                item.OwnerId = itemGiftDto.GiftTo;

                this.dbContext.Update(item);
                await this.dbContext.SaveChangesAsync();

                redisDatabase.KeyDelete($"{nameof(CharacterDto)}:{itemGiftDto.GiftTo}");

                logger.LogInformation("Item with Id = {id} gifted from {from} to {to}.", itemGiftDto.ItemId, itemGiftDto.GiftFrom, itemGiftDto.GiftTo);

                return this.StatusCode(StatusCodes.Status200OK, itemGiftDto);
            }
            catch (InvalidOperationException)
            {
                var message = "Gift :: Error when trying to get Item data, Item with Id = {id}, Owner = {owner} does not exist.";
                
                logger.LogError(message, itemGiftDto.ItemId, itemGiftDto.GiftFrom);

                return this.StatusCode(StatusCodes.Status400BadRequest, string.Format(message, itemGiftDto.ItemId, itemGiftDto.GiftFrom));
            }
            catch (Exception ex)
            {
                var message = "Gift :: Error when trying to get Item data, Item with Id = {id}, Owner = {owner}";
                var response = ex.Message;

                logger.LogError(ex, message, itemGiftDto.ItemId, itemGiftDto.GiftFrom);
                
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("{id:Guid}")]
        public ObjectResult Get(Guid id)
        {
            try
            {
                var response = GetItemFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                var response = $"Get :: Item with id = {id} not found.";
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Get :: Error when trying to get Item data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("GetByOwnerId/{id:Guid}")]
        public ObjectResult GetByOwnerId(Guid id)
        {
            try
            {
                var result = dbContext.Items.Include(x => x.BaseItem).Where(x => x.OwnerId == id);
                var response = mapper.Map<IEnumerable<ItemDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "GetByOwnerId :: Error when trying to get Item data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("GetByBaseItemId/{id:Guid}")]
        public ObjectResult GetByBaseItemId(Guid id)
        {
            try
            {
                var result = dbContext.Items.Include(x => x.BaseItem).Where(x => x.BaseItem.Id == id);
                var response = mapper.Map<IEnumerable<ItemDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "GetByBaseItemId :: Error when trying to get Item data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }

        }

        private async Task<ItemDto> GetItemFromCacheOrDb(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(ItemDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<ItemDto>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext
                            .Items
                            .FirstAsync(x => x.Id == id);

                var mapped = this.mapper.Map<ItemDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(ItemDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(15));

                return mapped;
            }
        }
    }
}

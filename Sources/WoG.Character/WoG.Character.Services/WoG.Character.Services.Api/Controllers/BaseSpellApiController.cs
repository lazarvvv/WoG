using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;
using WoG.Characters.Services.Api.Data;
using WoG.Characters.Services.Api.Models;
using WoG.Characters.Services.Api.Models.Dtos;
using WoG.Characters.Services.Api.Services;

namespace WoG.Characters.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseSpellApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<BaseItemApiController> logger;
        private readonly IMapper mapper;
        private readonly IDatabase redisDatabase;

        public BaseSpellApiController(ApplicationDbContext dbContext, IConnectionMultiplexer connectionMultiplexer,
            ILogger<BaseItemApiController> logger, IMapper mapper)
        {
            this.redisDatabase = connectionMultiplexer.GetDatabase();

            this.dbContext = dbContext;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet, Authorize(Roles = "GM")]
        public async Task<ObjectResult> Get()
        {
            try
            {
                var result = await dbContext.BaseItems.ToListAsync();
                var response = mapper.Map<IEnumerable<BaseItemDto>>(result);

                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error when trying to get Base Items");
                return this.StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("{id:Guid}")]
        public async Task<ObjectResult> Get(Guid id)
        {
            try
            {
                var response = await GetBaseSpellFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                var response = $"Base Item with id = {id} not found.";
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, $"Error occured when getting base item.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpPost, Authorize(Roles = "GM")]
        public async Task<ObjectResult> Post([FromBody] BaseItemDto baseItemDto)
        {
            try
            {
                var mapped = mapper.Map<BaseItem>(baseItemDto);

                await dbContext.BaseItems.AddAsync(mapped);
                await dbContext.SaveChangesAsync();

                return this.StatusCode(StatusCodes.Status200OK, mapped);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, $"Error occured when creating base item.");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private async Task<BaseSpellDto> GetBaseSpellFromCacheOrDb(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(BaseSpellDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<BaseSpellDto>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext
                            .BaseSpells
                            .FirstAsync(x => x.Id == id);

                var mapped = this.mapper.Map<BaseSpellDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(BaseSpellDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(10));

                return mapped;
            }
        }
    }
}

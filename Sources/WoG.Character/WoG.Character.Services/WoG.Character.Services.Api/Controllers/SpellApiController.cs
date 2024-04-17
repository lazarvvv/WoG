using AutoMapper;
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
using WoG.Characters.Services.Api.Models.Dtos;

namespace WoG.Characters.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpellApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<SpellApiController> logger;
        private readonly IMapper mapper;

        private readonly IDatabase redisDatabase;

        public SpellApiController(ApplicationDbContext dbContext, IConnectionMultiplexer connectionMultiplexer,
            ILogger<SpellApiController> logger, IMapper mapper)
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
                var result = await dbContext
                    .Spells
                    .Include(x => x.BaseSpell)
                    .ToListAsync();
                var response = mapper.Map<IEnumerable<SpellDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Get :: Error when trying to get Spell data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize]
        [Route("{id:Guid}")]
        public async Task<ObjectResult> Get(Guid id)
        {
            try
            {
                var idFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User not authenticated.");

                if (!idFromToken.Equals(id.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return this.StatusCode(StatusCodes.Status403Forbidden, $"User with id = {id} not authorized to view this resource");
                }

                var response = await GetSpellFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                var response = $"Get :: Spell with id = {id} not found.";
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Get :: Error when trying to get Spell data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize]
        [Route("GetByOwnerId/{id:Guid}")]
        public async Task<ObjectResult> GetByOwnerId(Guid id)
        {
            try
            {
                var idFromToken = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User not authenticated.");

                if (!idFromToken.Equals(id.ToString(), StringComparison.CurrentCultureIgnoreCase))
                {
                    return this.StatusCode(StatusCodes.Status403Forbidden, $"User with id = {id} not authorized to view this resource");
                }

                var response = await GetItemFromCacheOrDbByOwner(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "GetByOwnerId :: Error when trying to get Spell data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private async Task<SpellDto> GetSpellFromCacheOrDb(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(SpellDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<SpellDto>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext
                            .Spells
                            .Include(x => x.BaseSpell)
                            .FirstAsync(x => x.Id == id);

                var mapped = this.mapper.Map<SpellDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(SpellDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(15));

                return mapped;
            }
        }

        private async Task<IEnumerable<SpellDto>> GetItemFromCacheOrDbByOwner(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"ByOwner:{nameof(SpellDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<IEnumerable<SpellDto>>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext
                            .Spells
                            .Include(x => x.BaseSpell)
                            .FirstAsync(x => x.Id == id);

                var mapped = this.mapper.Map<IEnumerable<SpellDto>>(data);

                await redisDatabase.StringSetAsync($"ByOwner:{nameof(SpellDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(5));

                return mapped;
            }
        }
    }
}

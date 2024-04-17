using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using StackExchange.Redis;
using System.Linq;
using WoG.Characters.Services.Api.Data;
using WoG.Characters.Services.Api.Models;
using WoG.Characters.Services.Api.Models.Dtos;
using WoG.Characters.Services.Api.Services;

namespace WoG.Characters.Services.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CharacterApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<CharacterApiController> loggger;
        private readonly IMapper mapper;
        private readonly IDatabase redisDatabase;

        public CharacterApiController(ApplicationDbContext dbContext, IConnectionMultiplexer redisConnectionMultiplexer,
            ILogger<CharacterApiController> logger, IMapper mapper)
        {
            this.dbContext = dbContext;
            this.loggger = logger;
            this.mapper = mapper;

            this.redisDatabase = redisConnectionMultiplexer.GetDatabase();
        }

        [HttpGet, Authorize(Roles = "GM")]
        public async Task<ObjectResult> Get()
        {
            try
            {
                var result = await dbContext
                    .Characters
                    .Include(x => x.CharacterClass)
                    .Include(x => x.Items)
                    .ThenInclude(x => x.BaseItem)
                    .Include(x => x.Spells)
                    .ThenInclude(x => x.BaseSpell)
                    .ToListAsync();

                var response = mapper.Map<IEnumerable<CharacterDto>>(result);

                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to get Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpPost, Authorize]
        public async Task<ObjectResult> Post([FromBody] CharacterDto characterDto)
        {
            try
            {
                var result = mapper.Map<Character>(characterDto);
                await dbContext.AddAsync(result);
                await dbContext.SaveChangesAsync();

                return this.StatusCode(StatusCodes.Status200OK, result);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to Save Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet]
        [Route("{id:Guid}")]
        public async Task<ObjectResult> Get(Guid id)
        {
            try
            {
                var response = await GetCharacterFromCacheOrDb(id);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                var response = $"Character with id = {id} not found.";
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to get Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("GetByClassId/{id:Guid}")]
        public async Task<ObjectResult> GetByClassId(Guid id)
        {
            try
            {
                var result = await dbContext
                    .Characters
                    .Include(x => x.CharacterClass)
                    .Include(x => x.Items)
                    .ThenInclude(x => x.BaseItem)
                    .Include(x => x.Spells)
                    .ThenInclude(x => x.BaseSpell)
                    .Where(x => x.CharacterClass.Id == id)
                    .ToListAsync();

                var response = mapper.Map<IEnumerable<CharacterDto>>(result);
            
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to get Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("GetByClassName/{name}")]
        public async Task<ObjectResult> GetByClassName(string name)
        {
            try
            {
                var result = await dbContext
                    .Characters
                    .Include(x => x.CharacterClass)
                    .Include(x => x.Items)
                    .ThenInclude(x => x.BaseItem)
                    .Include(x => x.Spells)
                    .ThenInclude(x => x.BaseSpell)
                    .Where(x => x.CharacterClass.Name == name)
                    .ToListAsync();
                
                var response = mapper.Map<IEnumerable<CharacterDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to get Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }

        }

        [HttpGet, Authorize(Roles = "GM")]
        [Route("GetByAccountId/{id:Guid}")]
        public async Task<ObjectResult> GetByAccountId(Guid id)
        {
            try
            {
                var result = await dbContext
                    .Characters
                    .Include(x => x.CharacterClass)
                    .Include(x => x.Items)
                    .ThenInclude(x => x.BaseItem)
                    .Include(x => x.Spells)
                    .ThenInclude(x => x.BaseSpell)
                    .Where(x => x.CreatedBy == id)
                    .ToListAsync();

                var response = mapper.Map<IEnumerable<CharacterDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                loggger.LogError(ex, "Error when trying to get Character data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private async Task<CharacterDto> GetCharacterFromCacheOrDb(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(CharacterDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<CharacterDto>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext.Characters.Include(x => x.CharacterClass)
                            .Include(x => x.Items)
                            .ThenInclude(x => x.BaseItem)
                            .Include(x => x.Spells)
                            .ThenInclude(x => x.BaseSpell)
                            .FirstAsync(x => x.Id == id);
                var mapped = this.mapper.Map<CharacterDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(CharacterDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(5));

                return mapped;
            }
        }
    }
}

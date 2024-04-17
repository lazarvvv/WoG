using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    public class CharacterClassApiController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        private readonly ILogger<CharacterClassApiController> logger;
        private readonly IMapper mapper;
        private readonly IDatabase redisDatabase;

        public CharacterClassApiController(ApplicationDbContext dbContext, IConnectionMultiplexer redisConnectionMultiplexer,
            ILogger<CharacterClassApiController> logger, IMapper mapper)
        {
            this.redisDatabase = redisConnectionMultiplexer.GetDatabase();
            this.dbContext = dbContext;
            this.logger = logger;
            this.mapper = mapper;
        }

        [HttpGet, Authorize(Roles = "GM")]
        public ObjectResult Get()
        {
            try
            {
                var result = dbContext.CharacterClasses.ToList();
                var response = mapper.Map<IEnumerable<CharacterClassDto>>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Error when trying to get Character Class data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        [HttpGet, Authorize]
        [Route("{id:Guid}")]
        public async Task<ObjectResult> Get(Guid id)
        {
            try
            {
                var result = await dbContext.CharacterClasses.FirstAsync(x => x.Id == id);
                var response = mapper.Map<CharacterClassDto>(result);
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (InvalidOperationException)
            {
                var response = $"Character Class with id = {id} not found.";
                return this.StatusCode(StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                var response = ex.Message;
                logger.LogError(ex, "Error when trying to get Character Class data");
                return this.StatusCode(StatusCodes.Status500InternalServerError, response);
            }
        }

        private async Task<CharacterClassDto> GetCharacterClassFromCacheOrDb(Guid id)
        {
            var cachedData = await redisDatabase.StringGetAsync($"{nameof(CharacterClassDto)}:{id}");

            if (cachedData.HasValue)
            {
                return JsonConvert.DeserializeObject<CharacterClassDto>(cachedData!)!;
            }
            else
            {
                var data = await this.dbContext
                            .CharacterClasses
                            .FirstAsync(x => x.Id == id);

                var mapped = this.mapper.Map<CharacterClassDto>(data);

                await redisDatabase.StringSetAsync($"{nameof(CharacterDto)}:{id}",
                    JsonConvert.SerializeObject(mapped), expiry: TimeSpan.FromMinutes(15));

                return mapped;
            }
        }
    }
}

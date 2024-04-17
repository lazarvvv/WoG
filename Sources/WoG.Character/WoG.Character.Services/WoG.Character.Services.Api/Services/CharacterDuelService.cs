using AutoMapper;
using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Runtime.CompilerServices;
using WoG.Characters.Services.Api.Data;
using WoG.Characters.Services.Api.Models;
using WoG.Characters.Services.Api.Models.Dtos;
using WoG.Combat.Services.Api.Rpc;
using WoG.Core.RabbitMqCommunication;
using WoG.Core.RabbitMqCommunication.Interfaces;
using WoG.Core.RabbitMqCommunication.Requests;
using WoG.Core.RabbitMqCommunication.Responses;

namespace WoG.Characters.Services.Api.Services
{
    public class CharacterDuelService : BackgroundService
    {
        private readonly IMapper mapper;
        private readonly IMessageProducer<TwoCharacterInteractionRequest, QueueDuelResponse> messageProducer;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly IDatabase redisDatabase;

        public CharacterDuelService(IMapper mapper, IConfiguration appSettings,
            IConnectionMultiplexer connectionMultiplexer, IServiceScopeFactory serviceScopeFactory)
        {
            this.mapper = mapper;
            this.serviceScopeFactory = serviceScopeFactory;
            this.redisDatabase = connectionMultiplexer.GetDatabase();

            var hostName = appSettings.GetValue<string>("RabbitMq:Host") ??
                throw new InvalidConfigurationException("RabbitMq:Host cannot be null when initializing RabbitMq");

            var port = appSettings.GetValue<int>("RabbitMq:Port");

            var username = appSettings.GetValue<string>("RabbitMq:Username") ??
                throw new InvalidConfigurationException("RabbitMq:Username cannot be null when initializing RabbitMq");

            var password = appSettings.GetValue<string>("RabbitMq:Password") ??
                throw new InvalidConfigurationException("RabbitMq:Password cannot be null when initializing RabbitMq");

            var virtualHost = appSettings.GetValue<string>("RabbitMq:VirtualHost") ??
                throw new InvalidConfigurationException("RabbitMq:VirtualHost cannot be null when initializing RabbitMq");

            var requestQueueName = appSettings.GetValue<string>("RabbitMq:Queues:CharacterInfoRequestQueue") ??
                throw new InvalidConfigurationException("RabbitMq:Queues:CharacterInfoRequestQueue cannot be null when initializing RabbitMq");
            
            var replyQueueName = appSettings.GetValue<string>("RabbitMq:Queues:CharacterInfoReplyQueue") ??
                throw new InvalidConfigurationException("RabbitMq:Queues:CharacterInfoReplyQueue cannot be null when initializing RabbitMq");

            this.messageProducer = new MessageProducer<TwoCharacterInteractionRequest, QueueDuelResponse>(
                hostName, port, username, password, virtualHost, requestQueueName, replyQueueName)
            {
                OnReceive = async (request) => 
                {
                    using var scope = this.serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var challenger = await GetCharacterFromCacheOrDb(request.ChallengerId);
                    var defender = await GetCharacterFromCacheOrDb(request.DefenderId);

                    var duelQueueResult = new QueueDuelResponse()
                    {
                        ChallengerInfo = new CharacterInfo(challenger.Id, challenger.Strength + challenger.Agility,
                            challenger.Faith, challenger.Health, challenger.Mana),
                        ChallengerSpells = challenger
                            .Spells
                            .Select(x => 
                                new SpellInfo(x.Id, x.OwnerId, x.Name, x.ManaRequired, x.CooldownInSeconds, x.BaseHealing == 0 ? 0 : x.BaseHealing + challenger.Faith,
                                    x.BaseDamage == 0 ? 0 : x.BaseDamage + challenger.Intelligence))
                            .ToList(),
                        DefenderInfo = new CharacterInfo(defender.Id, defender.Strength + defender.Agility,
                            defender.Faith, defender.Health, defender.Mana),
                        DefenderSpells = defender
                            .Spells
                            .Select(x =>
                                new SpellInfo(x.Id, x.OwnerId, x.Name, x.ManaRequired, x.CooldownInSeconds, x.BaseHealing == 0 ? 0 : x.BaseHealing + challenger.Faith,
                                    x.BaseDamage == 0 ? 0 : x.BaseDamage + challenger.Intelligence))
                            .ToList(),
                    };

                    return duelQueueResult;
                }
            };
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
                var scope = serviceScopeFactory.CreateScope();
                using var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var data = await dbContext.Characters.Include(x => x.CharacterClass)
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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}

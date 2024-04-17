using AutoMapper;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.Configuration;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using WoG.Combat.Services.Api.Data;
using WoG.Combat.Services.Api.Models;
using WoG.Combat.Services.Api.Models.Dtos;
using WoG.Combat.Services.Api.Rpc;
using WoG.Core.RabbitMqCommunication;
using WoG.Core.RabbitMqCommunication.Helpers;
using WoG.Core.RabbitMqCommunication.Interfaces;
using WoG.Core.RabbitMqCommunication.Requests;
using WoG.Core.RabbitMqCommunication.Responses;
using static StackExchange.Redis.Role;

namespace WoG.Combat.Services.Api.Services
{
    public class CombatService : BackgroundService
    {
        private readonly ILogger<CombatService> logger;
        private readonly IMapper mapper;
        private readonly IMessageConsumer<TwoCharacterInteractionRequest, QueueDuelResponse> messageConsumer;
        private readonly IServiceScopeFactory serviceScopeFactory;
        private readonly int damageCooldownInSeconds;
        private readonly int healingCooldownInSeconds;
        private readonly int maximumDuelDurationInSeconds;

        public CombatService(ILogger<CombatService> logger, IConfiguration appSettings, IMapper mapper, IServiceScopeFactory serviceScopeFactory)
        {
            this.logger = logger;
            this.mapper = mapper;
            this.serviceScopeFactory = serviceScopeFactory;

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

            this.damageCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:DamageCooldownInSeconds");
            this.healingCooldownInSeconds = appSettings.GetValue<int>("CombatProperties:HealingCooldownInSeconds");
            this.maximumDuelDurationInSeconds = appSettings.GetValue<int>("CombatProperties:MaximumDuelDurationInSeconds");

            if (this.damageCooldownInSeconds == 0)
            {
                throw new InvalidConfigurationException("CombatProperties:DamageCooldownInSeconds cannot be 0.");
            }

            if(this.healingCooldownInSeconds == 0)
            {
                throw new InvalidConfigurationException("CombatProperties:HealingCooldownInSeconds cannot be 0.");
            }

            this.messageConsumer = new MessageConsumer<TwoCharacterInteractionRequest, QueueDuelResponse>(
                hostName, port, username, password, virtualHost, requestQueueName, replyQueueName)
            {
                OnReceived = async (response) =>
                {
                    var duelId = Guid.NewGuid();
                    var events = new List<DuelEventDto>() {
                        new DuelEventDto
                        {
                            DuelId = duelId,
                            Sequence = 0,
                            CharacterId = response.ChallengerInfo.Id,
                            EventType = Enums.EventType.Init,
                            TimeWhenNextActionOfTypeAvailable = DateTime.Now,
                            Damage = response.ChallengerInfo.Damage,
                            Healing = response.ChallengerInfo.Healing,
                            InitialHealth = response.ChallengerInfo.Health,
                            InitialMana = response.ChallengerInfo.Mana,
                        },
                        new DuelEventDto
                        {
                            DuelId = duelId,
                            Sequence = 1,
                            CharacterId = response.DefenderInfo.Id,
                            EventType = Enums.EventType.Init,
                            TimeWhenNextActionOfTypeAvailable = DateTime.Now,
                            InitialHealth = response.DefenderInfo.Health,
                            InitialMana = response.DefenderInfo.Mana
                        }
                    };

                    var spells = response.ChallengerSpells.Union(response.DefenderSpells).Select(x => new DuelSpell()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        OwnerId = x.OwnerId,
                        Damage = x.Damage,
                        Healing = x.Healing,
                        ManaCost = x.ManaCost,
                        CooldownInSeconds = x.CooldownInSeconds,
                    });

                    var duel = mapper.Map<Duel>(new DuelDto() 
                    {
                        Id = duelId,
                        ChallengerId = response.ChallengerInfo.Id,
                        DefenderId = response.DefenderInfo.Id,
                        DuelOutcome = Enums.DuelOutcome.Ongoing,
                        Events = events 
                    });

                    using var scope = serviceScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    await dbContext.Duels.AddAsync(duel);
                    await dbContext.Spells.AddRangeAsync(spells);

                    await dbContext.SaveChangesAsync();

                    response.DuelId = duelId;

                    return response;
                }
            };
        }

        public async Task<QueueDuelResponse> QueueDuel(Guid challengerId, Guid defenderId)
        {
            var response = await this.messageConsumer.PublishRequest(new TwoCharacterInteractionRequest() 
            {
                ChallengerId = challengerId,
                DefenderId = defenderId,
            });

            return response;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}

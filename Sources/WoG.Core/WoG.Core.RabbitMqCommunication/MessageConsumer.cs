using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WoG.Core.RabbitMqCommunication.Helpers;
using WoG.Core.RabbitMqCommunication.Interfaces;

namespace WoG.Core.RabbitMqCommunication
{
    public class MessageConsumer<TRequest, TResponse> : IMessageConsumer<TRequest, TResponse>, IDisposable where TResponse : class where TRequest : class
    {
        public required Func<TResponse, Task<TResponse>> OnReceived;

        private readonly IConnection Connection;
        private readonly IModel Channel;
        private readonly string ReplyQueueName;
        private readonly string RequestQueueName;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<TResponse>> CallbackMapper = new();

        public MessageConsumer(string hostName, int port, string username, string password, string virtualHost,
            string requestQueueName, string replyQueueName)
        {
            var factory = new ConnectionFactory()
            {
                HostName = hostName,
                Port = port,
                UserName = username,
                Password = password,
                VirtualHost = virtualHost
            };

            this.RequestQueueName = requestQueueName;
            this.ReplyQueueName = replyQueueName;
            this.Connection = factory.CreateConnection();
            this.Channel = this.Connection.CreateModel();

            this.Channel.QueueDeclare(this.RequestQueueName, exclusive: false);
            this.Channel.QueueDeclare(this.ReplyQueueName, exclusive: false);
            
            var consumer = new EventingBasicConsumer(this.Channel);

            consumer.Received += async (model, ea) =>
            {
                if (!this.CallbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var taskCompletionSource))
                {
                    return;
                }

                if (this.OnReceived == null)
                {
                    return;
                }

                var response = RpcSerializationHelper.FromByteArray<TResponse>(ea.Body.ToArray());
                var funcResponse = await this.OnReceived.Invoke(response);
                taskCompletionSource.TrySetResult(funcResponse ?? throw new Exception());
            };

            this.Channel.BasicConsume(consumer: consumer,
                                 queue: ReplyQueueName,
                                 autoAck: true);
        }

        public async Task<TResponse> PublishRequest(TRequest message, CancellationToken cancellationToken = default)
        {
            var props = this.Channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();
            props.CorrelationId = correlationId;
            props.ReplyTo = ReplyQueueName;
            
            var messageBytes = RpcSerializationHelper.ToByteArray(message);
            var taskCompletionSource = new TaskCompletionSource<TResponse>();

            this.CallbackMapper.TryAdd(correlationId, taskCompletionSource);

            this.Channel.BasicPublish(exchange: string.Empty,
                                 routingKey: this.RequestQueueName,
                                 basicProperties: props,
                                 body: messageBytes);

            cancellationToken.Register(() => this.CallbackMapper.TryRemove(correlationId, out _));

            return await taskCompletionSource.Task;
        }

        public void Dispose()
        {
            this.Connection.Close();
        }
    }
}

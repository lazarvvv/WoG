using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using WoG.Core.RabbitMqCommunication.Helpers;
using WoG.Core.RabbitMqCommunication.Interfaces;

namespace WoG.Combat.Services.Api.Rpc
{
    public class MessageProducer<TRequest, TResult> : IMessageProducer<TRequest, TResult> where TRequest : class where TResult : class
    {
        public required Func<TRequest, Task<TResult>> OnReceive;

        private readonly IConnection Connection;
        private readonly IModel Channel;
        private readonly string RequestQueueName;
        private readonly string ReplyQueueName;

        public MessageProducer(string hostName, int port, string username, string password, string virtualHost, string requestQueueName, string replyQueueName)
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
            this.Channel = Connection.CreateModel();

            this.Channel.QueueDeclare(queue: this.RequestQueueName, exclusive: false);
            this.Channel.QueueDeclare(queue: this.ReplyQueueName, exclusive: false);

            var consumer = new EventingBasicConsumer(this.Channel);
            consumer.Received += async (model, ea) =>
            {
                if(this.OnReceive == null)
                {
                    return;
                }

                var props = Channel.CreateBasicProperties();
                props.CorrelationId = ea.BasicProperties.CorrelationId;

                var fromBytes = RpcSerializationHelper.FromByteArray<TRequest>(ea.Body.ToArray());
                var reply = await this.OnReceive.Invoke(fromBytes);
                var toBytes = RpcSerializationHelper.ToByteArray(reply);

                this.Channel.BasicPublish(string.Empty, this.ReplyQueueName, props, toBytes);
            };

            this.Channel.BasicConsume(queue: this.RequestQueueName, autoAck: true, consumer: consumer);
        }

        public void PublishToQueue(TResult message)
        {
            var properties = this.Channel.CreateBasicProperties();
            properties.CorrelationId = Guid.NewGuid().ToString();
            properties.ReplyTo = this.ReplyQueueName;

            this.Channel.BasicPublish(
                string.Empty, this.ReplyQueueName, properties, RpcSerializationHelper.ToByteArray(message));
        }

        public void Close()
        {
            this.Connection.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoG.Core.RabbitMqCommunication.Interfaces
{
    public interface IMessageConsumer<TRequest, TResponse>
    {
        Task<TResponse> PublishRequest(TRequest message, CancellationToken cancellationToken = default);
    }
}

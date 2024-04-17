using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoG.Core.RabbitMqCommunication.Interfaces
{
    public interface IMessageProducer<TRequest, TResult>
    {
        void PublishToQueue(TResult message);
    }
}

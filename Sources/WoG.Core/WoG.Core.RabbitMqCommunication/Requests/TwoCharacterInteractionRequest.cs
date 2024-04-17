using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoG.Core.RabbitMqCommunication.Requests
{
    public class TwoCharacterInteractionRequest
    {
        public Guid ChallengerId { get; set; }
        public Guid DefenderId { get; set; }
    }
}

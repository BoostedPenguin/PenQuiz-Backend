﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameService.Helpers
{
    public class AppSettings
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string RabbitMQHost { get; set; }
        public string RabbitMQPort { get; set; }
        public string RabbitMQUserName { get; set; }
        public string RabbitMQPassword { get; set; }
        public string RabbitMQUri { get; set; }
        public string GrpcAccount { get; set; }
    }
}

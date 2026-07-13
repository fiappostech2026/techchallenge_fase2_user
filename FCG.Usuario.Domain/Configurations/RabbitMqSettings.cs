using System;
using System.Collections.Generic;
using System.Text;

namespace FCG.Usuario.Domain.Configurations
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = string.Empty;
        public string VirtualHost { get; set; } = "/";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

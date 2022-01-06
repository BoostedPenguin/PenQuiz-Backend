using AccountService.Dtos;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AccountService.MessageBus
{
    public interface IMessageBusClient
    {
        void PublishNewUser(UserCreatedDto userCreatedDto);
    }

    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConnection connection;
        private readonly IModel channel;

        public MessageBusClient(IOptions<AppSettings> appSettings)
        {
            var factory = new ConnectionFactory()
            {
                HostName = appSettings.Value.RabbitMQHost,
                Port = int.Parse(appSettings.Value.RabbitMQPort),
            };

            if(appSettings.Value.RabbitMQPassword != "")
            {
                factory.Password = appSettings.Value.RabbitMQPassword;
            }
            if (appSettings.Value.RabbitMQUserName != "")
            {
                factory.UserName = appSettings.Value.RabbitMQUserName;
            }
            if (appSettings.Value.RabbitMQUri != "")
            {
                factory.Uri = new Uri(appSettings.Value.RabbitMQUri);
            }

            try
            {
                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);

                connection.ConnectionShutdown += Connection_ConnectionShutdown;

                Console.WriteLine("--> Connected to MessageBus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RabbitMQ Connection Shutdown");
        }

        public void PublishNewUser(UserCreatedDto userCreatedDto)
        {
            var message = JsonSerializer.Serialize(userCreatedDto);

            if (connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
                SendMessage(message);
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connectionis closed, not sending");
            }
        }

        private void SendMessage(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "trigger",
                            routingKey: "",
                            basicProperties: null,
                            body: body);

            Console.WriteLine($"--> We have sent {message}");
        }

        public void Dispose()
        {
            Console.WriteLine("MessageBus Disposed");
            if (channel.IsOpen)
            {
                channel.Close();
                connection.Close();
            }
        }
    }
}

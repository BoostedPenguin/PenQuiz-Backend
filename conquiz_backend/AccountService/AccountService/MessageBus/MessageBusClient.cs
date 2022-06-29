using AccountService.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
        void PublishUserCharacter(UserCharacterEventDto response);
    }

    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly IWebHostEnvironment env;

        public string AccountsExchange { get; }

        public MessageBusClient(IOptions<AppSettings> appSettings, IWebHostEnvironment env)
        {
            var factory = new ConnectionFactory()
            {
                HostName = appSettings.Value.RabbitMQHost,
                Port = int.Parse(appSettings.Value.RabbitMQPort),
            };


            if (!string.IsNullOrWhiteSpace(appSettings.Value.RabbitMQPassword))
            {
                factory.Password = appSettings.Value.RabbitMQPassword;
            }
            if (!string.IsNullOrWhiteSpace(appSettings.Value.RabbitMQUserName))
            {
                factory.UserName = appSettings.Value.RabbitMQUserName;
            }
            if (!string.IsNullOrWhiteSpace(appSettings.Value.RabbitMQUri))
            {
                factory.Uri = new Uri(appSettings.Value.RabbitMQUri);
            }

            try
            {

                if (env.IsProduction())
                {
                    AccountsExchange = "account_events";
                }
                else
                {
                    AccountsExchange = "dev_account_events";
                }

                connection = factory.CreateConnection();
                channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: AccountsExchange, type: ExchangeType.Direct);

                connection.ConnectionShutdown += Connection_ConnectionShutdown;

                Console.WriteLine("--> Connected to MessageBus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the Message Bus: {ex.Message}");
            }

            this.env = env;
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

        public void PublishUserCharacter(UserCharacterEventDto response)
        {
            var message = JsonSerializer.Serialize(response);

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

            var rk = "account_response";

            if (!env.IsProduction())
            {
                rk = "dev_account_response";
            }

            channel.BasicPublish(exchange: AccountsExchange,
                            routingKey: rk,
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

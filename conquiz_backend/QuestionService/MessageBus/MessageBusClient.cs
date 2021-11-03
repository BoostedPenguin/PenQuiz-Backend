using AccountService.Dtos;
using Microsoft.Extensions.Options;
using QuestionService.Dtos;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QuestionService.MessageBus
{
    public interface IMessageBusClient
    {
        void PublishRequestedQuestions(QuestionResponse questionResponseDto);
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

            try
            {
                connection = factory.CreateConnection();

                channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "question_events", type: ExchangeType.Direct);

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

        public void PublishRequestedQuestions(QuestionResponse questionResponseDto)
        {
            var message = JsonSerializer.Serialize(questionResponseDto);

            if (connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");
                SendMessage(message, "question_response");
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connectionis closed, not sending");
            }
        }

        private void SendMessage(string message, string rk)
        {
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "question_events",
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

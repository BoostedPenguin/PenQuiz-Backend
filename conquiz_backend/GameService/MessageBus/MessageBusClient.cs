using GameService.Dtos;
using GameService.Helpers;
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

namespace GameService.MessageBus
{
    public interface IMessageBusClient
    {
        void RequestQuestions(RequestQuestionsDto requestQuestionsDto);
    }

    public class MessageBusClient : IMessageBusClient, IDisposable
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly IWebHostEnvironment env;
        private readonly string QuestionsExchange;
        public MessageBusClient(IOptions<AppSettings> appSettings, IWebHostEnvironment env)
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

                if(env.IsProduction())
                {
                    QuestionsExchange = "question_events";
                }
                else
                {
                    QuestionsExchange = "dev_question_events";
                }

                channel.ExchangeDeclare(exchange: QuestionsExchange, type: ExchangeType.Direct);

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

        public void RequestQuestions(RequestQuestionsDto requestQuestionsDto)
        {
            var message = JsonSerializer.Serialize(requestQuestionsDto);

            if (connection.IsOpen)
            {
                Console.WriteLine("--> RabbitMQ Connection Open, sending message...");

                if(env.IsProduction())
                {
                    SendMessage(message, "question_request");
                }
                else
                {
                    SendMessage(message, "dev_question_request");
                }
            }
            else
            {
                Console.WriteLine("--> RabbitMQ connectionis closed, not sending");
            }
        }

        private void SendMessage(string message, string rk)
        {
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: QuestionsExchange,
                            routingKey: rk,
                            basicProperties: null,
                            body: body);

            Console.WriteLine($"--> We have sent a DIRECT msg with: {rk}");
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

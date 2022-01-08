using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using QuestionService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuestionService.MessageBus
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IEventProcessor eventProcessor;
        private readonly IOptions<AppSettings> appSettings;
        private readonly IWebHostEnvironment env;
        private IConnection connection;
        private IModel channel;
        private string queue;
        private string QuestionsExchange;

        public MessageBusSubscriber(IEventProcessor eventProcessor, IOptions<AppSettings> appSettings, IWebHostEnvironment env)
        {
            this.eventProcessor = eventProcessor;
            this.appSettings = appSettings;
            this.env = env;
            InitRabbbitMQ();
        }

        private void InitRabbbitMQ()
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

            this.connection = factory.CreateConnection();
            this.channel = connection.CreateModel();

            if (env.IsProduction())
            {
                QuestionsExchange = "question_events";

                channel.ExchangeDeclare(exchange: QuestionsExchange, type: ExchangeType.Direct);
                queue = channel.QueueDeclare().QueueName;

                // List all "event types" that you're interested in to listen to
                channel.QueueBind(queue, QuestionsExchange, "question_request");
            }
            else
            {
                QuestionsExchange = "dev_question_events";

                channel.ExchangeDeclare(exchange: QuestionsExchange, type: ExchangeType.Direct);
                queue = channel.QueueDeclare().QueueName;

                // List all "event types" that you're interested in to listen to
                channel.QueueBind(queue, QuestionsExchange, "dev_question_request");
            }



            Console.WriteLine("--> Listening on the Message Bus..");

            connection.ConnectionShutdown += Connection_ConnectionShutdown;
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> Connection Shutdown");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (ModuleHandle, ea) =>
            {
                Console.WriteLine("--> Event Received!");

                var body = ea.Body;
                var notificationMessage = Encoding.UTF8.GetString(body.ToArray());

                eventProcessor.ProcessEvent(notificationMessage);
            };

            channel.BasicConsume(queue, true, consumer);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            if (channel.IsOpen)
            {
                channel.Close();
                connection.Close();
            }

            base.Dispose();
        }
    }
}

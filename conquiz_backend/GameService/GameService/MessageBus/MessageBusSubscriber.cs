using GameService.EventProcessing;
using GameService.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameService.MessageBus
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IEventProcessor eventProcessor;
        private readonly IOptions<AppSettings> appSettings;
        private readonly IWebHostEnvironment env;
        private  IConnection connection;
        private IModel channel;
        private string queue;

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

            if (appSettings.Value.RabbitMQPassword != "")
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

            this.connection = factory.CreateConnection();
            this.channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
            queue = channel.QueueDeclare().QueueName;
            
            channel.QueueBind(queue, "trigger", "");

            if(env.IsProduction())
            {
                channel.QueueBind(queue, "question_events", "question_response");
            }
            else
            {
                channel.QueueBind(queue, "dev_question_events", "dev_question_response");
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
            if(channel.IsOpen)
            {
                channel.Close();
                connection.Close();
            }

            base.Dispose();
        }
    }
}

﻿using GameService.EventProcessing;
using GameService.Helpers;
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
        private  IConnection connection;
        private IModel channel;
        private string queue;

        public MessageBusSubscriber(IEventProcessor eventProcessor, IOptions<AppSettings> appSettings)
        {
            this.eventProcessor = eventProcessor;
            this.appSettings = appSettings;

            InitRabbbitMQ();
        }

        private void InitRabbbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = appSettings.Value.RabbitMQHost,
                Port = int.Parse(appSettings.Value.RabbitMQPort),
            };
            this.connection = factory.CreateConnection();
            this.channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "trigger", type: ExchangeType.Fanout);
            queue = channel.QueueDeclare().QueueName;
            
            channel.QueueBind(queue, "trigger", "");
            channel.QueueBind(queue, "question_events", "question_response");

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
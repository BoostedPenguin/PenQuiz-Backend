﻿using GameService.Dtos;
using GameService.Dtos.SignalR_Responses;
using GameService.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        void RequestQuestions(RequestCapitalQuestionsDto requestQuestionsDto);
        void RequestFinalNumberQuestion(RequestFinalNumberQuestionDto requestFinalNumberDto);
        void SendNewCharacter(CharacterResponse character);
    }

    public class MessageBusClient : IMessageBusClient, IDisposable
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<MessageBusClient> logger;
        private readonly string QuestionsExchange;
        public MessageBusClient(IOptions<AppSettings> appSettings, IWebHostEnvironment env, ILogger<MessageBusClient> logger)
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

                logger.LogInformation("Connected to MessageBus");
            }
            catch (Exception ex)
            {
                logger.LogError($"Could not connect to the Message Bus: {ex.Message}");
            }

            this.env = env;
            this.logger = logger;
        }

        private void Connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            logger.LogInformation($"RabbitMQ Connection Shutdown");
        }

        private void SendMessageToAccount(object requestDto)
        {
            var message = JsonSerializer.Serialize(requestDto);

            if (env.IsProduction())
            {
                SendMessage(message, "account_events", "account_request");
            }
            else
            {
                SendMessage(message, "dev_account_events", "dev_account_request");
            }
        }

        private void SendMessageToQuestion(object requestDto)
        {
            var message = JsonSerializer.Serialize(requestDto);

            if (env.IsProduction())
            {
                SendMessage(message, "question_events", "question_request");
            }
            else
            {
                SendMessage(message, "dev_question_events", "dev_question_request");
            }
        }


        public void SendNewCharacter(CharacterResponse character)
        {
            SendMessageToAccount(character);
        }

        public void RequestQuestions(RequestQuestionsDto requestQuestionsDto)
        {
            SendMessageToQuestion(requestQuestionsDto);
        }

        public void RequestQuestions(RequestCapitalQuestionsDto requestQuestionsDto)
        {
            SendMessageToQuestion(requestQuestionsDto);
        }

        public void RequestFinalNumberQuestion(RequestFinalNumberQuestionDto requestQuestionsDto)
        {
            SendMessageToQuestion(requestQuestionsDto);
        }

        private void SendMessage(string message, string exchange, string rk)
        {
            if (connection.IsOpen)
            {
                logger.LogDebug($"RabbitMQ Connection Open, sending message...");

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: exchange,
                                routingKey: rk,
                                basicProperties: null,
                                body: body);

                logger.LogInformation($"We have sent a DIRECT msg with: {rk}");
            }
            else
            {
                logger.LogWarning($"RabbitMQ connectionis closed, not sending message.");
            }
        }

        public void Dispose()
        {
            logger.LogInformation($"MessageBus Disposed");
            if (channel.IsOpen)
            {
                channel.Close();
                connection.Close();
            }
        }
    }
}

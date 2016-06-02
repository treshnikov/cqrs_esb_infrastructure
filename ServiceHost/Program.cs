﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CQRS;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using WebApplication;
using WebApplication.Controllers;

namespace ServiceHost
{
    class Program
    {
        private static readonly string ServiceName = "userService";

        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: ServiceName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    var rk = ea.RoutingKey;

                    var message = JsonConvert.DeserializeObject<EsbMessageBody>(Encoding.UTF8.GetString(body));
                    Console.WriteLine(" [x] Received {0} routing key:  {1} reply to: {2} corrId: {3}", message.Body, rk, ea.BasicProperties.ReplyTo, ea.BasicProperties.CorrelationId);

                    var container = UnityControllerFactory.ConfigContainer();
                    if (message.Header.EndsWith("Command"))
                    {
                        var commandInstance = CqrsControllerBase.GetCommandInstance(message.Header, message.Body);
                        CqrsControllerBase.ExcecuteCommand(commandInstance);
                    }
                    else if (message.Header.EndsWith("Query"))
                    {
                        var queryInstance = CqrsControllerBase.GetQueryInstance(message.Header, message.Body);
                        var result = CqrsControllerBase.ExecuteQuery(queryInstance);

                        if (ea.BasicProperties.ReplyTo != null)
                        {
                            var replyProps = channel.CreateBasicProperties();
                            replyProps.CorrelationId = ea.BasicProperties.CorrelationId;
                            channel.BasicPublish("",
                                ea.BasicProperties.ReplyTo,
                                replyProps,
                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result)));

                            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                    }
                    else
                    {
                        return;
                    }

                };

                channel.BasicConsume(
                    queue: ServiceName,
                    noAck: false,
                    consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();

            }
        }
    }
}

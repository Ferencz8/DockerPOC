using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerPOC.Amqp
{
    public class RabbitMqService : IDisposable
    {
        public readonly string OrdersExchange = "OrdersExchange";
        public readonly string OrdersQueue = "OrdersQueue";
        public readonly string OrdersQueueAndExchangeRoutingKey = "OrdersRouting";
        private readonly RabbitMqConfiguration _configuration;
        private IConnection _connection;
        private bool _disposedValue;
        public IModel Channel { get; private set; }
        public RabbitMqService(IOptions<RabbitMqConfiguration> options)
        {
            _configuration = options.Value;
            Connect();
        }
        private void Connect()
        {
            if (_connection == null || _connection.IsOpen == false)
            {
                Console.WriteLine("Creating Connection");
                ConnectionFactory connection = new ConnectionFactory()
                {
                    UserName = _configuration.Username,
                    Password = _configuration.Password,
                    HostName = _configuration.HostName
                };
                connection.DispatchConsumersAsync = true;
                _connection = connection.CreateConnection();
            }


            if (Channel == null || Channel.IsOpen == false)
            {
                Console.WriteLine("Creating Channel");
                Channel = _connection.CreateModel();
                Channel.ExchangeDeclare(exchange: OrdersExchange, type: "direct", durable: true, autoDelete: false);
                Channel.QueueDeclare(queue: OrdersQueue, durable: false, exclusive: false, autoDelete: false);
                Channel.QueueBind(queue: OrdersQueue, exchange: OrdersExchange, routingKey: OrdersQueueAndExchangeRoutingKey);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Channel?.Close();
                    Channel?.Dispose();
                    Channel = null;

                    _connection?.Close();
                    _connection?.Dispose();
                    _connection = null;
                }

                _disposedValue = true;
            }
        }
    }
}

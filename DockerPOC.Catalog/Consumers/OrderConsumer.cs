using DockerPOC.Amqp;
using DockerPOC.API.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DockerPOC.Catalog.Consumers
{
    public class OrderConsumer : RabbitMqService, IHostedService
    {
        private readonly ILogger<OrderConsumer> _logger;
        private readonly IConnectionMultiplexer _redis;
        private const string ProductsKey = "Products";

        public OrderConsumer(IConnectionMultiplexer redis, IOptions<RabbitMqConfiguration> options, ILogger<OrderConsumer> logger)
            : base(options)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task Handle(Order order)
        {
            var db = _redis.GetDatabase();
            var productsJson = await db.StringGetAsync(ProductsKey);
            var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);
            var orderedProduct = products.FirstOrDefault(n => n.Id == order.Id);

            if (orderedProduct == null)
            {
                _logger.LogError($"Order Id {order.Id} not found");
                throw new ArgumentOutOfRangeException(nameof(order.Id));
            }
            if (orderedProduct.StockCount == 0)
            {
                _logger.LogError($"Order Id {order.Id} is out of stock");
                throw new ArgumentOutOfRangeException(nameof(order.Id));
            }

            orderedProduct.StockCount--;

            await db.StringSetAsync(ProductsKey, JsonConvert.SerializeObject(products));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                _logger.LogInformation("Started OrderConsumer");

                var consumer = new AsyncEventingBasicConsumer(Channel);                
                consumer.Received += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation(" Received message with body: {0}", message);
                    await Handle(JsonConvert.DeserializeObject<Order>(message));
                };
                Channel.BasicConsume(queue: OrdersQueue, autoAck: false, consumer: consumer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"OrderConsumer exception with StackTrace: {ex.StackTrace} Msg: {ex.Message}");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class Order
    {
        public int Id { get; set; }

        public int Price { get; set; }
    }
}

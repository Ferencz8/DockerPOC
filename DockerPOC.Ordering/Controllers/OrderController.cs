using DockerPOC.Amqp;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DockerPOC.Ordering.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {

        private readonly RabbitMqService _rabbitMqService;

        public OrderController(RabbitMqService rabbitMqService)
        {
            _rabbitMqService = rabbitMqService;
        }

        // POST api/<ValuesController>
        [HttpPost]
        public IActionResult SendOrder([FromBody] Order order)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(order));
            var properties = _rabbitMqService.Channel.CreateBasicProperties();
            properties.ContentType = "application/json";
            _rabbitMqService.Channel.BasicPublish(exchange: _rabbitMqService.OrdersExchange,
                                 _rabbitMqService.OrdersQueueAndExchangeRoutingKey,
                                 basicProperties: properties,
                                 body: body);

            return Ok();
        }
    }

    public class Order
    {
        public int Id { get; set; }

        public int Price { get; set; }
    }
}

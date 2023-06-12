using Microsoft.AspNetCore.Mvc;
using Products.DTO;
 using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Collections.Generic;
using RabbitMQ.Client;
using System.Text;

namespace Products.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class productController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
                 => Ok("Product Service is running!");

        [HttpPost]
        public async Task<IActionResult> Order([FromBody] OrderDTO dto)
        {
            //check if the values being posted in postman is Null or empty price and qty has to be checked to 0 because postman automatically posts empty
            //intergers or decimals to 0 
            //Also checks if qty is less than 0
            if ((string.IsNullOrEmpty(dto.Item) ) || (dto.Price.Equals(0)) || (dto.Qty.Equals(0)) || (dto.Qty <= 0)) 
            {
                return Ok("Invalid Request");
            }
           

            Debug.WriteLine(dto.Item + dto.Price + dto.Qty);
            // Connect to RabbitMQ in your container
            var factory = new ConnectionFactory
            {
                Uri = new Uri("amqp://guest:guest@localhost:5672")
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Connect to the "price-moved" queue on RabbitMQ
            channel.QueueDeclare(
                "Order-Placed",
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Serialize the MessageDto object into a json string
            var OrderEventAdded = JsonConvert.SerializeObject(dto);
            // Encode the json string into UTF8
            var body = Encoding.UTF8.GetBytes(OrderEventAdded);
            // Publish the message to the queue
            channel.BasicPublish("", "Order-Placed", null, body);

            return Ok("Order Placed");
        }


    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Costing.Database;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Costing.DTO;
using Costing.Database.Modals;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Costing.BackGroundService
{
    public class CostingBackgroundService :BackgroundService
    {
        private ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        private readonly IServiceScopeFactory _scopeFactory;

        private const string QueueName = "Order-Placed";


        public CostingBackgroundService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }



        public override Task StartAsync(CancellationToken cancellationToken)
        {

            _connectionFactory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest"
            };
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(QueueName,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.BasicQos(0, 1, false);


            return base.StartAsync(cancellationToken);
        }


        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();


            var timer = new Timer(CheckMessages, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            return Task.CompletedTask;
        }



        private async void CheckMessages(object state)
        {

            var consumer = new EventingBasicConsumer(_channel);


            consumer.Received += async (sender, evnt) =>
            {

                var body = evnt.Body.ToArray();
                var priceMovedEventData = JsonConvert.DeserializeObject<Message>(Encoding.UTF8.GetString(body));
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetService<InventoryDBContext>();
                

                List<CostRecords> CostList = await dbContext.Cost.ToListAsync();

               CostRecords tRec = new CostRecords();
               var costprice = ((float)priceMovedEventData.Price) * 70 / 100;
                {
                    tRec.Item = priceMovedEventData.Item;
                    tRec.Price = ((float)priceMovedEventData.Price);
                    tRec.Qty = priceMovedEventData.Qty;
                    tRec.cost_price = costprice;
                };

                

                Debug.WriteLine(priceMovedEventData.Item);
                dbContext.Add(tRec);
                await dbContext.SaveChangesAsync();
            };

            _channel.BasicConsume(QueueName, true, consumer);
        }


    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DockerPOC.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private const string ProductsKey = "Products";

        public ProductController(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] Product product)
        {
            try
            {
                var db = _redis.GetDatabase();
                var productsKeyExists = await db.KeyExistsAsync(ProductsKey);
                if (!productsKeyExists)
                {
                    await db.StringSetAsync(ProductsKey, "[]");
                }
                var productsJson = await db.StringGetAsync(ProductsKey);
                var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);
                products.Add(product);
                await db.StringSetAsync(ProductsKey, JsonConvert.SerializeObject(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, JsonConvert.SerializeObject(ex));
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetProduct([FromQuery] string productName)
        {
            try
            {
                var db = _redis.GetDatabase();

                var productsJson = await db.StringGetAsync(ProductsKey);
                var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);
                var queriedProduct = products.Where(n => n.Name == productName);

                return Ok(queriedProduct);
            }
            catch (Exception ex)
            {
                return StatusCode(500, JsonConvert.SerializeObject(ex));
            }
        }
    }

    public class Product
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int Price { get; set; }

        public int StockCount { get; set; }
    }
}

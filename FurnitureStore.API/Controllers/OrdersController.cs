using FurnitureStore.Data;
using FurnitureStore.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FurnitureStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly FurnitureStoreContext _context;
        public OrdersController(FurnitureStoreContext context)
        {
           _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var orders = await _context.Orders.Include(o=>o.OrderDetails).ToListAsync();
            return Ok(orders);
        }
        [HttpGet("GetWithProducts")]
        public async Task<IActionResult> GetWithProducts()
        {
            var orders = await _context.Orders.Include(o => o.OrderDetails).ToListAsync();
            //var orderWithProductInfo = new
            //{
            //    order.Id,
            //    order.OrderNumber,
            //    order.ClientId,
            //    order.OrderDate,
            //    order.DeliveryDate,
            //    OrderDetails = order.OrderDetails.Select(od => new
            //    {
            //        od.OrderId,
            //        od.ProductId,
            //        od.Quantity,
            //        Product = _context.Products.FirstOrDefault(p => p.Id == od.ProductId)
            //    })
            //};

            //return Ok(orderWithProductInfo);
            var ordersWithProductInfo = orders.Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.ClientId,
                order.OrderDate,
                order.DeliveryDate,
                OrderDetails = order.OrderDetails.Select(od => new
                {
                    od.OrderId,
                    od.ProductId,
                    od.Quantity,
                    Product = _context.Products.FirstOrDefault(p => p.Id == od.ProductId)
                })
            });

            return Ok(ordersWithProductInfo);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).SingleOrDefaultAsync(x=>x.Id == id);
            if (order == null) NotFound();
            return Ok(order);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Order order)
        {
            if(order.OrderDetails == null) BadRequest("Order should have at least one detail");
            await _context.Orders.AddAsync(order);
            await _context.Orders.AddRangeAsync(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Post), order.Id, order);

        }

        [HttpPut]
        public async Task<IActionResult> Put(Order order)
        {
            if (order.OrderDetails == null) BadRequest("Order should have at least one detail");
            var existingOrder = await _context.Orders.Include(o => o.OrderDetails)
                .SingleOrDefaultAsync(x => x.Id == order.Id);
            if (existingOrder == null) NotFound();

            existingOrder.OrderNumber = order.OrderNumber;
            existingOrder.OrderDate = order.OrderDate;
            existingOrder.DeliveryDate = order.DeliveryDate;
            existingOrder.ClientId = order.ClientId;

            _context.OrderDetails.RemoveRange(existingOrder.OrderDetails);
            _context.Orders.Update(existingOrder);
            _context.OrderDetails.AddRange(order.OrderDetails);
            await _context.SaveChangesAsync();

            return NoContent();

        }
        [HttpDelete]
        public async Task<IActionResult> Delete(int Id)
        {
            var existingOrder = await _context.Orders.Include(o => o.OrderDetails)
                .SingleOrDefaultAsync(x => x.Id == Id);
            if (existingOrder == null) NotFound();

            _context.OrderDetails.RemoveRange(existingOrder.OrderDetails);
            _context.Orders.RemoveRange(existingOrder);
            await _context.SaveChangesAsync();

            return NoContent();

        }

    }
}

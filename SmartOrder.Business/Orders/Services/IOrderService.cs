using SmartOrder.Entities.Orders.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartOrder.Business.Orders.Services
{
    public interface IOrderService
    {
        Task<IEnumerable<OrderDto>> GetAllOrdersAsync();
        Task<IEnumerable<OrderDto>> GetOrdersByDateAsync(DateTime date);
        Task<OrderDto> GetOrderByIdAsync(int id);
        Task<OrderDto> CreateOrderAsync(OrderDto order);
    }
}

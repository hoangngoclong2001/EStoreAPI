using BusinessObject.Models;
using DataAccess.DAO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.RepoImpl
{
    public class OrderRepo : IOrderRepo
    {
        public Task<Order> Order(int? id) => OrderDAO.Order(id);

        public Task<List<Order>> Orders(PaginationParams @params, DateTime? from, DateTime? to) => OrderDAO.Orders(@params, from, to);

        public Task<List<Order>> Orders(PaginationParams @params, DateTime? from, DateTime? to, string customerId) => OrderDAO.Orders(@params, from, to, customerId);

        public async Task<List<Order>> Orders(DateTime? from, DateTime? to, string? customerId) => await OrderDAO.GetAll(from, to, customerId);

        public Task<bool> Save(Order order) => OrderDAO.Save(order);

        public Task<bool> Update(Order order) => OrderDAO.Update(order);

        public Task<bool> Delete(Order order) => OrderDAO.Delete(order);
    }
}

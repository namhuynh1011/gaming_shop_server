using gaming_shop_api.Models;
using gaming_shop_server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace gaming_shop_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public OrderAPIController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] OrderRequestDto request)
        {
            // Validate
            if (request.Items == null || !request.Items.Any())
                return BadRequest("Giỏ hàng trống!");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Bạn cần đăng nhập để đặt hàng.");
            }
            var order = new Order
            {
                UserId = userId, // SỬA Ở ĐÂY
                FullName = request.FullName,
                Phone = request.Phone,
                Address = request.Address,
                Note = request.Note,
                PaymentMethod = request.PaymentMethod,
                Status = request.PaymentMethod == "cod" ? "Pending" : "Unpaid",
                OrderDate = DateTime.UtcNow,
                TotalAmount = request.Items.Sum(i => i.ProductPrice * i.Quantity),
                Items = request.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductPrice = i.ProductPrice,
                    Quantity = i.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (request.PaymentMethod == "cod")
            {
                return Ok(new { message = "Đặt hàng thành công! Thanh toán khi nhận hàng." });
            }
            else if (request.PaymentMethod == "vnpay")
            {

                // Chuyển sang trang demo chính thức của VNPAY
                return Redirect("http://sandbox.vnpayment.vn/tryitnow/Home/CreateOrder");

                // HOẶC nếu thích view mã QR tĩnh (tạo view DemoVnpayQr.cshtml)
                // return View("DemoVnpayQr");
            }
            else
            {
                return BadRequest("Phương thức thanh toán không hợp lệ!");
            }
        }
    }

    // DTO giữ nguyên
    public class OrderRequestDto
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }
    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
    }
}
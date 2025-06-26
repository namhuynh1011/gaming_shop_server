using gaming_shop_api.Models;
using gaming_shop_server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using VNPAY.NET;
using VNPAY.NET.Enums;
using VNPAY.NET.Models;

namespace gaming_shop_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderAPIController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnpay _vnpay;
        private readonly IConfiguration _config;
        public OrderAPIController(ApplicationDbContext context, IVnpay vnpay, IConfiguration config)
        {
            _context = context;
            _vnpay = vnpay;
            _config = config;
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
                UserId = userId,
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
                var vnpayUrl = CreateVnpayPaymentUrl(order.Id, order.TotalAmount, "Thanh toán đơn hàng #" + order.Id);
                return Ok(new { redirectUrl = vnpayUrl });
            }
            else if (request.PaymentMethod == "momo")
            {
                var momoUrl = await CreateMomoPaymentUrl(order.Id, order.TotalAmount, "Thanh toán đơn hàng #" + order.Id);
                return Ok(new { redirectUrl = momoUrl });
            }
            else
            {
                return BadRequest("Phương thức thanh toán không hợp lệ!");
            }
        }
        [HttpGet("admin/orders")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            // Có thể thêm kiểm tra quyền admin nếu cần
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }

        // Thêm API: Lấy đơn hàng theo ID (dành cho admin)
        [HttpGet("admin/orders/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            // Có thể thêm kiểm tra quyền admin nếu cần
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            return Ok(order);
        }
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetUserOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Bạn cần đăng nhập để xem đơn hàng.");
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpGet("my-orders/{id}")]
        public async Task<IActionResult> GetUserOrderById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Bạn cần đăng nhập để xem đơn hàng.");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            return Ok(order);
        }
        [HttpPut("admin/orders/{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
        {
            // TODO: Kiểm tra quyền admin hoặc nhân viên ở đây nếu có phân quyền

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound("Không tìm thấy đơn hàng.");

            // Có thể kiểm tra hợp lệ trạng thái ở đây nếu muốn
            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái thành công.", status = order.Status });
        }
        private string CreateVnpayPaymentUrl(int orderId, decimal amount, string description)
        {
            var tmnCode = _config["Vnpay:TmnCode"];
            var hashSecret = _config["Vnpay:HashSecret"];
            var vnpUrl = _config["Vnpay:BaseUrl"];
            var returnUrl = _config["Vnpay:ReturnUrl"];
            _vnpay.Initialize(tmnCode, hashSecret, vnpUrl, returnUrl);
            var payRequest = new PaymentRequest
            {
                PaymentId = orderId, // Nếu PaymentId là mã đơn hàng
                Description = description,
                Money = (double)amount,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                //BankCode = BankCode.VNPAYQR, // enum
                CreatedDate = DateTime.Now,
                Currency = Currency.VND,     // enum
                Language = DisplayLanguage.Vietnamese// enum
            };


            string paymentUrl = _vnpay.GetPaymentUrl(payRequest);
            return paymentUrl;
        }
        private async Task<string> CreateMomoPaymentUrl(int orderId, decimal amount, string description)
        {
            var configSection = _config.GetSection("Momo");
            var endpoint = "https://test-payment.momo.vn/v2/gateway/api/create";
            var partnerCode = configSection["PartnerCode"];
            var accessKey = configSection["AccessKey"];
            var secretKey = configSection["SecretKey"];
            var returnUrl = configSection["ReturnUrl"];
            var notifyUrl = configSection["NotifyUrl"];
            var orderInfo = description;
            var orderIdStr = orderId.ToString();
            var requestId = Guid.NewGuid().ToString();
            var amountStr = ((long)amount).ToString(); // MoMo nhận VND, không nhân 100

            var rawHash = $"accessKey={accessKey}&amount={amountStr}&extraData=&ipnUrl={notifyUrl}&orderId={orderIdStr}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType=captureWallet";
            string signature;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawHash));
                signature = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            var request = new
            {
                partnerCode,
                accessKey,
                requestId,
                amount = amountStr,
                orderId = orderIdStr,
                orderInfo,
                redirectUrl = returnUrl,
                ipnUrl = notifyUrl,
                extraData = "",
                requestType = "captureWallet",
                signature
            };

            using (var client = new HttpClient())
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();
                dynamic responseObj = JsonConvert.DeserializeObject(responseString);
                return responseObj.payUrl;
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
    public class UpdateOrderStatusDto
    {
        public string Status { get; set; }
    }
}
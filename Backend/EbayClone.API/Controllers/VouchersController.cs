using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Vouchers;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Vouchers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EbayClone.API.Controllers
{
    [ApiController]
    [Route("api/vouchers")]
    [Authorize]
    public class VouchersController : ControllerBase
    {
        private readonly CreateVoucherUseCase _createUseCase;
        private readonly GetVouchersUseCase _getListUseCase;
        private readonly GetVoucherByIdUseCase _getByIdUseCase;
        private readonly UpdateVoucherUseCase _updateUseCase;
        private readonly UpdateVoucherStatusUseCase _updateStatusUseCase;
        private readonly DeleteVoucherUseCase _deleteUseCase;
        private readonly ApplyVoucherUseCase _applyUseCase;
        private readonly IShopRepository _shopRepository;

        public VouchersController(
            CreateVoucherUseCase createUseCase,
            GetVouchersUseCase getListUseCase,
            GetVoucherByIdUseCase getByIdUseCase,
            UpdateVoucherUseCase updateUseCase,
            UpdateVoucherStatusUseCase updateStatusUseCase,
            DeleteVoucherUseCase deleteUseCase,
            ApplyVoucherUseCase applyUseCase,
            IShopRepository shopRepository)
        {
            _createUseCase = createUseCase;
            _getListUseCase = getListUseCase;
            _getByIdUseCase = getByIdUseCase;
            _updateUseCase = updateUseCase;
            _updateStatusUseCase = updateStatusUseCase;
            _deleteUseCase = deleteUseCase;
            _applyUseCase = applyUseCase;
            _shopRepository = shopRepository;
        }

        // Helper: lấy ShopId của seller đang đăng nhập
        private async Task<Guid> GetSellerShopIdAsync()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _shopRepository.GetByUserIdAsync(userId);
            if (shop == null) throw new InvalidOperationException("Seller chưa có shop.");
            return shop.Id;
        }

        // ── GET /api/vouchers?status=ACTIVE ──────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? status = null)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                var vouchers = await _getListUseCase.ExecuteAsync(shopId, status);
                return Ok(vouchers.Select(MapToDto));
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── GET /api/vouchers/{id} ────────────────────────────────────────
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                var voucher = await _getByIdUseCase.ExecuteAsync(id, shopId);
                return Ok(MapToDto(voucher));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException ex) { return Forbid(); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── POST /api/vouchers ────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVoucherRequest request)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                var voucher = await _createUseCase.ExecuteAsync(shopId, request);
                return CreatedAtAction(nameof(GetById), new { id = voucher.Id }, MapToDto(voucher));
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── PUT /api/vouchers/{id} ────────────────────────────────────────
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVoucherRequest request)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                await _updateUseCase.ExecuteAsync(id, shopId, request);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── PATCH /api/vouchers/{id}/status ──────────────────────────────
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateVoucherStatusRequest request)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                await _updateStatusUseCase.ExecuteAsync(id, shopId, request.Status);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── DELETE /api/vouchers/{id} ─────────────────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var shopId = await GetSellerShopIdAsync();
                await _deleteUseCase.ExecuteAsync(id, shopId);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── POST /api/vouchers/apply ──────────────────────────────────────
        // Buyer gọi để PREVIEW discount trước khi đặt hàng.
        // KHÔNG tiêu lượt voucher — gọi PreviewAsync (không AtomicApply).
        // AtomicApply thật sự xảy ra TRONG CreateTestOrderUseCase khi buyer confirm mua.
        [HttpPost("apply")]
        public async Task<IActionResult> Apply([FromBody] ApplyVoucherPreviewRequest request)
        {
            try
            {
                var buyerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                // Preview: validate đầy đủ nhưng KHÔNG tiêu lượt voucher
                var result = await _applyUseCase.PreviewAsync(
                    request.Code,
                    request.ShopId,
                    buyerId,
                    request.ItemSubtotal,
                    request.ProductIds ?? new System.Collections.Generic.List<Guid>());
                return Ok(result);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ── Mapper ────────────────────────────────────────────────────────
        private static VoucherDto MapToDto(Voucher v) => new VoucherDto
        {
            Id = v.Id,
            ShopId = v.ShopId,   // [FIX-HIGH] cần cho Frontend PreviewDiscount (truyền shopId)
            Code = v.Code,
            Name = v.Name,
            DiscountType = v.DiscountType,
            Value = v.Value,
            MaxDiscountAmount = v.MaxDiscountAmount,
            MinOrderValue = v.MinOrderValue,
            MaxBudget = v.MaxBudget,
            UsedBudget = v.UsedBudget,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount,
            PerBuyerLimit = v.PerBuyerLimit,
            Visibility = v.Visibility,
            Scope = v.Scope,
            ProductIds = v.ProductIds,
            Status = v.Status,
            ValidFrom = v.ValidFrom,
            ValidTo = v.ValidTo
        };
    }
}

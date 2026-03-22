using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Vouchers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Vouchers
{
    public class GetVouchersUseCase
    {
        private readonly IVoucherRepository _voucherRepo;

        public GetVouchersUseCase(IVoucherRepository voucherRepo)
        {
            _voucherRepo = voucherRepo;
        }

        public async Task<List<Voucher>> ExecuteAsync(Guid shopId, string? statusFilter = null)
        {
            return await _voucherRepo.GetByShopIdAsync(shopId, statusFilter);
        }
    }

    public class GetVoucherByIdUseCase
    {
        private readonly IVoucherRepository _voucherRepo;

        public GetVoucherByIdUseCase(IVoucherRepository voucherRepo)
        {
            _voucherRepo = voucherRepo;
        }

        public async Task<Voucher> ExecuteAsync(Guid id, Guid shopId)
        {
            var voucher = await _voucherRepo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("Không tìm thấy voucher.");
            if (voucher.ShopId != shopId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập voucher này.");
            return voucher;
        }
    }
}

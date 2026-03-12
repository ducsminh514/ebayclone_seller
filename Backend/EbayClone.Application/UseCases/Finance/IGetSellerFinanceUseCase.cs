using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Finance;

namespace EbayClone.Application.UseCases.Finance
{
    public interface IGetSellerFinanceUseCase
    {
        Task<SellerFinanceDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}

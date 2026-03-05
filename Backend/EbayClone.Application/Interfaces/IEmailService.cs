using System.Threading.Tasks;

namespace EbayClone.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendVerificationEmailAsync(string to, string token);
    }
}

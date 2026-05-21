namespace ClaudyGod.Application.Common.Interfaces;

public interface IPayPalService
{
    string GenerateDonationUrl(decimal amount, string currency);
}

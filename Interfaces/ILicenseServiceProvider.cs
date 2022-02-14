namespace BrokenCode.Interfaces
{
    public interface ILicenseServiceProvider
    {
        ILicenseService GetLicenseService();
        ILicenseService GetLicenseService(int timeOut);
    }
}

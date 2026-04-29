// UpsPoC.Api/Services/IUpsDataService.cs
using UpsPoC.Api.Models;

namespace UpsPoC.Api.Services;

public interface IUpsDataService
{
    UpsStatus GetLatestStatus();
    List<UpsSnapshot> GetHistory();
    void UpdateStatus(UpsStatus status);
}

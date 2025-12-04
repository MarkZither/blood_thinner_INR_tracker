using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using BloodThinnerTracker.Mobile.ViewModels;

namespace BloodThinnerTracker.Mobile.Services
{
    public interface IInrService
    {
        Task<IEnumerable<InrListItemVm>> GetRecentAsync(int count = 5);
    }
}

using AutoMapper;
using net_core_backend.ViewModel;
using net_core_backend.Models;
using System.Linq;
using System.Collections.ObjectModel;

namespace net_core_backend.Profiles
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<DefaultModel, ExampleViewModel>();
        }
    }
}

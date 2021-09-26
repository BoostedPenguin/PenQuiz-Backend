using System;
using System.Collections.Generic;

namespace net_core_backend.Models
{
    public partial class MapObject
    {
        public int Id { get; set; }
        public int? Mapid { get; set; }
        public int? GameInstanceId { get; set; }
    }
}

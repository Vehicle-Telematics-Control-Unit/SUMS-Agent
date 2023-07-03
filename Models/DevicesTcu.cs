using System;
using System.Collections.Generic;

namespace SUMS_Agent.Models
{
    public partial class DevicesTcu
    {
        public string DeviceId { get; set; } = null!;
        public long TcuId { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }

        public virtual Device Device { get; set; } = null!;
        public virtual Tcu Tcu { get; set; } = null!;
    }
}

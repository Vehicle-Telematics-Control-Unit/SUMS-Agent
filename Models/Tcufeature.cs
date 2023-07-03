using System;
using System.Collections.Generic;

namespace SUMS_Agent.Models
{
    public partial class Tcufeature
    {
        public long TcuId { get; set; }
        public long FeatureId { get; set; }
        public bool? IsActive { get; set; }

        public virtual Feature Feature { get; set; } = null!;
        public virtual Tcu Tcu { get; set; } = null!;
    }
}

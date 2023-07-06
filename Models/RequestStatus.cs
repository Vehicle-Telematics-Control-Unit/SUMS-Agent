using System;
using System.Collections.Generic;

namespace SUMS_Agent.Models
{
    public partial class RequestStatus
    {
        public RequestStatus()
        {
            ConnectionRequests = new HashSet<ConnectionRequest>();
        }

        public long StatusId { get; set; }
        public string Description { get; set; } = null!;

        public virtual ICollection<ConnectionRequest> ConnectionRequests { get; set; }
    }
}

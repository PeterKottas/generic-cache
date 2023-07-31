using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finbourne.GenericCache.Core.Model.Enum
{
    public enum CacheDeletionReasonEnum
    {
        ManualDelete = 1,
        Purge = 2,
        CapacityReached = 4
    }
}

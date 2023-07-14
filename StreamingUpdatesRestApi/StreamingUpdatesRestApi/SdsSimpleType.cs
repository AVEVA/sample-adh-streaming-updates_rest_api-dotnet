using OSIsoft.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamingUpdatesRestApi
{
    /// <summary>
    /// Represents a simple time index type.
    /// </summary>
    public class SdsSimpleType
    {
        [SdsMember(IsKey = true)]
        public DateTimeOffset Time { get; set; }

        public double Value { get; set; }
    }
}

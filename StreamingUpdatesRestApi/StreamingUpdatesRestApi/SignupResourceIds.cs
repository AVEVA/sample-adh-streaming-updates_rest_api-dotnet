using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamingUpdatesRestApi
{
    public class SignupResourceIds
    {
        public IEnumerable<string> AccessibleResources { get; set; }

        public IEnumerable<string> InaccessibleResources { get; set; }
    }
}

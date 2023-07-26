using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamingUpdatesRestApi
{
    public class SignupResourcesInput
    {
        public IEnumerable<string>? ResourcesToAdd { get; set; }

        public IEnumerable<string>? ResourcesToRemove { get; set; }
    }
}

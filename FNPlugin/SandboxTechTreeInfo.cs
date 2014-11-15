using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    internal class SandboxTechTreeInfo : ITechInfoProvider
    {
        public bool IsAvailable(String techId)
        {
            return true;
        }
    }
}

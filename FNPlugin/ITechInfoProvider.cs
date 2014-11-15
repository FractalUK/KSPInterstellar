using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface ITechInfoProvider
    {
        bool IsAvailable(String techId);
    }
}

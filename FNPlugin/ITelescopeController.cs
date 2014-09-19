using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public interface ITelescopeController
    {
        bool CanProvideTelescopeControl { get; }
    }
}

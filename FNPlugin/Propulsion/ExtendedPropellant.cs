using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public class ExtendedPropellant : Propellant
    {
        private string _secondaryPropellantName;
        public string StoragePropellantName
        {
            get { return _secondaryPropellantName; }
        }

        public new void Load(ConfigNode node)
        {
            base.Load(node);

            _secondaryPropellantName = node.HasValue("storageName") ? node.GetValue("storageName") : name;
        }
    }
}

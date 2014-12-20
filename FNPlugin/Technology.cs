using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public static class Technology
    {
        private static bool _tech_in_use;
        private static ITechInfoProvider _tech_info_provider;

        public static bool TechnologyIsInUse { get { return (HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX); } }

        public static ITechInfoProvider TechInfoProvider
        {
            get
            {
                if (_tech_in_use == Technology.TechnologyIsInUse && _tech_info_provider != null)
                {
                    return _tech_info_provider;
                } else if (Technology.TechnologyIsInUse)
                {
                    _tech_in_use = Technology.TechnologyIsInUse;
                    _tech_info_provider = new CareerTechTreeInfo();
                    return _tech_info_provider;
                } else
                {
                    _tech_in_use = Technology.TechnologyIsInUse;
                    _tech_info_provider = new SandboxTechTreeInfo();
                    return _tech_info_provider;
                }
            }
        }
    }
}

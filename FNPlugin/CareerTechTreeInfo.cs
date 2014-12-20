using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    internal class CareerTechTreeInfo : ITechInfoProvider
    {
        
        private const string RNDSRING = "ResearchAndDevelopment";

        private readonly string _persistent_file_path;
        private Dictionary<string, bool> _tech_available;

        public CareerTechTreeInfo()
        {
            _tech_available = new Dictionary<string, bool>();
            _persistent_file_path = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/persistent.sfs";
        }

        public bool IsAvailable(String techId)
        {
            if (_tech_available.ContainsKey(techId)) return _tech_available[techId];

            try
            {
                ConfigNode config = ConfigNode.Load(_persistent_file_path);
                if (config == null) return false;
                ConfigNode gameconf = config.GetNode("GAME");
                if (gameconf == null) return false;
                ConfigNode[] scenarios = gameconf.GetNodes("SCENARIO");
                if (!scenarios.Any()) return false;
                ConfigNode tech_scenario = scenarios.FirstOrDefault(scn => scn.GetValue("name") == RNDSRING);
                if (tech_scenario == null) return false;
                ConfigNode[] techs = tech_scenario.GetNodes("Tech");
                if (techs.Any(tech => tech.HasValue("id") && tech.GetValue("id") == techId)) return true;
            } catch (System.IO.IOException)
            {
                return false;
            }
            return false;
        }
    }
}

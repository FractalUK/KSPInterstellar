using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    public enum ElectricEngineType 
    {
        PLASMA = 1,
        ARCJET = 2,
        VASIMR = 4,
        VACUUMTHRUSTER = 8,
        RCS = 16
    }

    public class ElectricEnginePropellant 
    {
        protected int prop_type;
        protected double efficiency;
        protected float ispMultiplier;
        protected float thrustMultiplier;
        protected float thrustMultiplierCold;
        protected Propellant propellant;
        protected String propellantname;
        protected String propellantguiname;
        protected String effectname;
        protected double wasteheatMultiplier;
        protected string techRquirement;

        public int SupportedEngines { get { return prop_type;} }

        public double Efficiency { get { return efficiency; } }

        public float IspMultiplier { get { return ispMultiplier; } }

        public float ThrustMultiplier { get { return thrustMultiplier; } }

        public float ThrustMultiplierCold { get { return thrustMultiplierCold; } }

        public Propellant Propellant {  get { return propellant; } }

        public String PropellantName { get { return propellantname; } }

        public String PropellantGUIName { get { return propellantguiname; } }

        public String ParticleFXName { get { return effectname; } }

        public double WasteHeatMultiplier { get { return wasteheatMultiplier; } }

        public string TechRequirement { get { return techRquirement; } }

        public ElectricEnginePropellant(ConfigNode node) 
        {
            propellantname = node.GetValue("name");
            propellantguiname = node.GetValue("guiName");
            ispMultiplier = Convert.ToSingle(node.GetValue("ispMultiplier"));
            thrustMultiplier = node.HasValue("thrustMultiplier") ? Convert.ToSingle(node.GetValue("thrustMultiplier")) : 1;
            thrustMultiplierCold = node.HasValue("thrustMultiplierCold") ? Convert.ToSingle(node.GetValue("thrustMultiplierCold")) : thrustMultiplier;
            wasteheatMultiplier = node.HasValue("wasteheatMultiplier") ? Convert.ToDouble(node.GetValue("wasteheatMultiplier")) : 1;
            efficiency = Convert.ToDouble(node.GetValue("efficiency"));
            prop_type = Convert.ToInt32(node.GetValue("type"));
            effectname = node.GetValue("effectName");
            techRquirement = node.HasValue("techRequirement") ? node.GetValue("techRequirement") : String.Empty;
            ConfigNode propellantnode = node.GetNode("PROPELLANT");
            propellant = new Propellant();
            propellant.Load(propellantnode);
        }


        public static List<ElectricEnginePropellant> GetPropellantsEngineForType(int type)
        {
            ConfigNode[] propellantlist = GameDatabase.Instance.GetConfigNodes("ELECTRIC_PROPELLANT");
            List<ElectricEnginePropellant> propellant_list;
            if (propellantlist.Length == 0)
            {
                PluginHelper.showInstallationErrorMessage();
                propellant_list = new List<ElectricEnginePropellant>();
            }
            else
            {
                propellant_list = propellantlist.Select(prop => new ElectricEnginePropellant(prop))
                    .Where(eep => (eep.SupportedEngines & type) == type && PluginHelper.HasTechRequirmentOrEmpty(eep.TechRequirement)).ToList();
            }

            return propellant_list;
        }
        
    }
}

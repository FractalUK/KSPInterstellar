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
        VACUUMTHRUSTER = 8
    }

    public class ElectricEnginePropellant 
    {
        protected int prop_type;
        protected double efficiency;
        protected double ispMultiplier;
        protected double thrustMultiplier;
        protected Propellant propellant;
        protected String propellantname;
        protected String propellantguiname;
        protected String effectname;
        protected double wasteheatMultiplier;

        public int SupportedEngines { get { return prop_type;} }

        public double Efficiency { get { return efficiency; } }

        public double IspMultiplier { get { return ispMultiplier; } }

        public double ThrustMultiplier { get { return thrustMultiplier; } }

        public Propellant Propellant {  get { return propellant; } }

        public String PropellantName { get { return propellantname; } }

        public String PropellantGUIName { get { return propellantguiname; } }

        public String ParticleFXName { get { return effectname; } }

        public double WasteHeatMultiplier { get { return wasteheatMultiplier; } }

        public ElectricEnginePropellant(ConfigNode node) 
        {
            propellantname = node.GetValue("name");
            propellantguiname = node.GetValue("guiName");
            ispMultiplier = Convert.ToDouble(node.GetValue("ispMultiplier"));
            thrustMultiplier = node.HasValue("thrustMultiplier") ? Convert.ToDouble(node.GetValue("thrustMultiplier")) : 1;
            wasteheatMultiplier = node.HasValue("wasteheatMultiplier") ? Convert.ToDouble(node.GetValue("wasteheatMultiplier")) : 1;
            efficiency = Convert.ToDouble(node.GetValue("efficiency"));
            prop_type = Convert.ToInt32(node.GetValue("type"));
            effectname = node.GetValue("effectName");
            ConfigNode propellantnode = node.GetNode("PROPELLANT");
            propellant = new Propellant();
            propellant.Load(propellantnode);
        }
        
    }
}

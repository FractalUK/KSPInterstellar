using System;
using System.Collections.Generic;
using System.Linq;

namespace FNPlugin 
{
    class ReactorProduction
    {
        public ReactorProduct fuelmode;
        public double mass;
    }


    class ReactorFuelMode 
	{
        protected int _reactor_type;
        protected string _mode_gui_name;
        protected string _techRequirement;
        protected List<ReactorFuel> _fuels;
        protected List<ReactorProduct> _products;
        protected float _reactionRate;
        protected float _powerMultiplier;
        protected float _normpowerrequirements;
        protected float _charged_power_ratio;
        protected double _mev_per_charged_product;
        protected double _neutrons_ratio;
        protected double _fuel_efficency_multiplier;
        protected bool _requires_lab;
        protected bool _requires_upgrade;

        public ReactorFuelMode(ConfigNode node) 
        {
            _reactor_type = Convert.ToInt32(node.GetValue("ReactorType"));
            _mode_gui_name = node.GetValue("GUIName");
            _techRequirement = node.HasValue("TechRequirement") ? node.GetValue("TechRequirement") : String.Empty;

            _reactionRate = node.HasValue("NormalisedReactionRate") ? Single.Parse(node.GetValue("NormalisedReactionRate")) : 1;
            _powerMultiplier = node.HasValue("NormalisedPowerMultiplier") ? Single.Parse(node.GetValue("NormalisedPowerMultiplier")) : 1;
            _normpowerrequirements = node.HasValue("NormalisedPowerConsumption") ? Single.Parse(node.GetValue("NormalisedPowerConsumption")) : 1;
            _charged_power_ratio = Single.Parse(node.GetValue("ChargedParticleRatio"));

            _mev_per_charged_product = node.HasValue("MeVPerChargedProduct") ? Double.Parse(node.GetValue("MeVPerChargedProduct")) : 0;
            _neutrons_ratio = node.HasValue("NeutronsRatio") ? Double.Parse(node.GetValue("NeutronsRatio")) : 1;
            _fuel_efficency_multiplier = node.HasValue("FuelEfficiencyMultiplier") ? Double.Parse(node.GetValue("FuelEfficiencyMultiplier")) : 1;
            _requires_lab = node.HasValue("RequiresLab") ? Boolean.Parse(node.GetValue("RequiresLab")) : false;
            _requires_upgrade = node.HasValue("RequiresUpgrade") ? Boolean.Parse(node.GetValue("RequiresUpgrade")) : false;

            ConfigNode[] fuel_nodes = node.GetNodes("FUEL");
            _fuels = fuel_nodes.Select(nd => new ReactorFuel(nd)).ToList();

            ConfigNode[] products_nodes = node.GetNodes("PRODUCT");
            _products = products_nodes.Select(nd => new ReactorProduct(nd)).ToList();
        }

        public int SupportedReactorTypes { get { return _reactor_type; } }

        public string ModeGUIName { get { return _mode_gui_name; } }

        public string TechRequirement  { get { return _techRequirement; } }

        public IList<ReactorFuel> ReactorFuels { get { return _fuels; } }

        public IList<ReactorProduct> ReactorProducts { get { return _products; } }

        public bool Aneutronic { get { return _neutrons_ratio == 0; } }

        public bool RequiresLab { get { return _requires_lab; } }

        public bool RequiresUpgrade { get { return _requires_upgrade; } }

        public float ChargedPowerRatio { get { return _charged_power_ratio; } }

        public double MeVPerChargedProduct { get { return _mev_per_charged_product; } }

        public float NormalisedReactionRate { get { return _reactionRate * _powerMultiplier; } }

        public float NormalisedPowerRequirements { get { return _normpowerrequirements; } }

        public double NeutronsRatio { get { return _neutrons_ratio; } }

        public double FuelEfficencyMultiplier { get { return _fuel_efficency_multiplier; } }
    }
}

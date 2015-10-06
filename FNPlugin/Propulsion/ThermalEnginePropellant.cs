using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Propulsion
{
    public class ThermalEnginePropellant
    {
        private string _fuelmode;

        private bool _isLFO;
        private bool _is_jet;

        private float _propellantSootFactorFullThrotle;
        private float _propellantSootFactorMinThrotle;
        private float _propellantSootFactorEquilibrium;
        private float _minDecompositionTemp;
        private float _maxDecompositionTemp;
        private float _decompositionEnergy;
        private float _baseIspMultiplier;
        private float _fuelToxicity;
        private float _ispPropellantMultiplier;
        private float _thrustPropellantMultiplier;
        

        public string Fuelmode { get {return _fuelmode;}}
        public float PropellantSootFactorFullThrotle { get { return _propellantSootFactorFullThrotle; } }
        public float PropellantSootFactorMinThrotle { get { return _propellantSootFactorMinThrotle; } }
        public float PropellantSootFactorEquilibrium { get { return _propellantSootFactorEquilibrium; } }
        public float MinDecompositionTemp { get { return _minDecompositionTemp; } }
        public float MaxDecompositionTemp { get { return _maxDecompositionTemp; } }
        public float DecompositionEnergy { get { return _decompositionEnergy; } }
        public float BaseIspMultiplier { get { return _baseIspMultiplier; } }
        public float FuelToxicity { get { return _fuelToxicity; } }
        public bool IsLFO { get { return _isLFO; } }
        public bool IsJet { get { return _is_jet; } }
        public float IspPropellantMultiplier { get { return _ispPropellantMultiplier; } }
        public float ThrustPropellantMultiplier { get { return _thrustPropellantMultiplier; } }

        public void Load(ConfigNode node)
        {
            _fuelmode = node.GetValue("guiName");
            _isLFO = node.HasValue("isLFO") ? bool.Parse(node.GetValue("isLFO")) : false;
            _is_jet = node.HasValue("isJet") ? bool.Parse(node.GetValue("isJet")) : false;
            _propellantSootFactorFullThrotle = node.HasValue("maxSootFactor") ? float.Parse(node.GetValue("maxSootFactor")) : 0;
            _propellantSootFactorMinThrotle = node.HasValue("minSootFactor") ? float.Parse(node.GetValue("minSootFactor")) : 0;
            _propellantSootFactorEquilibrium = node.HasValue("levelSootFraction") ? float.Parse(node.GetValue("levelSootFraction")) : 0;
            _minDecompositionTemp = node.HasValue("MinDecompositionTemp") ? float.Parse(node.GetValue("MinDecompositionTemp")) : 0;
            _maxDecompositionTemp = node.HasValue("MaxDecompositionTemp") ? float.Parse(node.GetValue("MaxDecompositionTemp")) : 0;
            _decompositionEnergy = node.HasValue("DecompositionEnergy") ? float.Parse(node.GetValue("DecompositionEnergy")) : 0;
            _baseIspMultiplier = node.HasValue("BaseIspMultiplier") ? float.Parse(node.GetValue("BaseIspMultiplier")) : 0;
            _fuelToxicity = node.HasValue("Toxicity") ? float.Parse(node.GetValue("Toxicity")) : 0;
            
            _ispPropellantMultiplier = node.HasValue("ispMultiplier") ? float.Parse(node.GetValue("ispMultiplier")) : 1;
            _thrustPropellantMultiplier = node.HasValue("thrustMultiplier") ? float.Parse(node.GetValue("thrustMultiplier")) : 1;
        }

    }
}

using OpenResourceSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin 
{
    class ReactorFuel 
    {
        protected double _fuel_usege_per_mw;
        protected string _fuel_name;
        protected double _density;
        protected string _unit;
        protected bool _consumeGlobal;

        public ReactorFuel(ConfigNode node) 
        {
            _fuel_name = node.GetValue("name");
            _fuel_usege_per_mw = Convert.ToDouble(node.GetValue("UsagePerMW"));
            _unit = node.GetValue("Unit");
            _density = PartResourceLibrary.Instance.GetDefinition(_fuel_name).density;
            _consumeGlobal = node.HasValue("consumeGlobal") ? Boolean.Parse(node.GetValue("consumeGlobal")) : true;
        }

        public bool ConsumeGlobal { get { return _consumeGlobal; } }

        public double Density { get { return _density; } }

        public double FuelUsePerMJ { get { return _fuel_usege_per_mw/_density; } }

        public double EnergyDensity { get { return 0.001/_fuel_usege_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string Unit { get { return _unit; } }

        public double GetFuelUseForPower(double efficiency, double megajoules, double fuelUsePerMJMult)
        {
            return FuelUsePerMJ * fuelUsePerMJMult * megajoules / efficiency;
        }

    }

    class ReactorProduct
    {
        protected double _product_usege_per_mw;
        protected string _fuel_name;
        protected double _density;
        protected string _unit;
        protected bool _produceGlobal;

        public ReactorProduct(ConfigNode node)
        {
            _fuel_name = node.GetValue("name");
            _density = PartResourceLibrary.Instance.GetDefinition(_fuel_name).density;
            _product_usege_per_mw = Convert.ToDouble(node.GetValue("ProductionPerMW"));
            _unit = node.GetValue("Unit");
            _produceGlobal = node.HasValue("produceGlobal") ? Boolean.Parse(node.GetValue("produceGlobal")) : true;
        }

        public bool ProduceGlobal { get { return _produceGlobal; } }

        public double Density { get { return _density; } }

        public double ProductUsePerMJ { get { return _product_usege_per_mw / _density; } }

        public double EnergyDensity { get { return 0.001 / _product_usege_per_mw; } }

        public string FuelName { get { return _fuel_name; } }

        public string Unit { get { return _unit; } }

        public double GetProductionForPower(double efficiency, double megajoules, double productionPerMJMult)
        {
            return ProductUsePerMJ * productionPerMJMult * megajoules / efficiency;
        }

    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin
{
    public sealed class RadiationDose
    {
        public double BetaDose { get; private set; }
        public double GammaDose { get; private set; }
        public double ProtonDose { get; private set; }
        public double NeutronDose { get; private set; }

        public double ChargedDose { get { return BetaDose + ProtonDose; } }
        public double TotalDose { get { return BetaDose + GammaDose + ProtonDose + NeutronDose; } }

        public RadiationDose(double beta_dose, double gamma_dose, double proton_dose, double neutron_dose)
        {
            this.BetaDose = beta_dose;
            this.GammaDose = gamma_dose;
            this.ProtonDose = proton_dose;
            this.NeutronDose = neutron_dose;
        }

        public RadiationDose GetDoseWithMaterialShielding(double factor)
        {
            return new RadiationDose(BetaDose * factor * 0.1, GammaDose * factor, ProtonDose, NeutronDose);
        }

        public RadiationDose GetDoseWithMagneticShielding(double factor)
        {
            return new RadiationDose(0.0, GammaDose, ProtonDose * factor, NeutronDose);
        }

        public override string ToString()
        {
            return "{ Beta = " + BetaDose.ToString("E") + " Gamma " + GammaDose.ToString("E") + " Proton " + ProtonDose.ToString("E") + " Neutron " + NeutronDose.ToString("E") + "}";
        }
    }
}

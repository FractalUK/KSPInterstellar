extern alias ORSvKSPIE;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSvKSPIE::OpenResourceSystem;

namespace FNPlugin 
{
    class VanAllen 
    {
        public const double B0 = 3.12E-5;
		public static Dictionary<string,double> crew_rad_exposure = new Dictionary<string, double> ();
    }
}

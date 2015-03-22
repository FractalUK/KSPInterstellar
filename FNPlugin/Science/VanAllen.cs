extern alias ORSv1_4_3;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ORSv1_4_3::OpenResourceSystem;

namespace FNPlugin {
    class VanAllen {
        public const double B0 = 3.12E-5;
		public static Dictionary<string,double> crew_rad_exposure = new Dictionary<string, double> ();
    }
}

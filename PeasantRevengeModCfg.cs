using BastardsMod.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeasantRevenge
{
    public class PeasantRevengeModCfg: XmlCfg
    {
        public PeasantRevengeConfiguration values = new PeasantRevengeConfiguration();

        public override object Load(string file_name, Type type)
        {
            values = (PeasantRevengeConfiguration)base.Load(file_name, type);
            return values;
        }
    }
}

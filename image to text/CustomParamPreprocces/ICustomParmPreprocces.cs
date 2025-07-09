using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace image_to_text.CustomParamPreprocces
{
    public interface ICustomParmPreprocces
    {
        string Type { get; set; }
        string Name { get; set; }
    }
}

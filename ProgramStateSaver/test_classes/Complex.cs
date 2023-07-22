using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramStateSaver
{
    internal class Complex : Saveable
    {
        [Save]
        public double RealPart { get; set; } = 2;
        [Save("Imaginary")]
        public double ImaginaryPart { get; set; } = 3;
    }
}

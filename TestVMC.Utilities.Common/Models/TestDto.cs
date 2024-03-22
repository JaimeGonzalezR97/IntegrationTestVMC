using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestVMC.Utilities.Common.Models
{
    public class TestDto
    {
        public string TestName { get; set; }
        public DateTime DateTime { get; set; }
        public bool RejectStatus { get; set; }
        public string TestStatus { get; set; }
        public string TestData { get; set; } //Is like temporary datum by form
        public string IntegrationSatatus { get; set; } = "Not executed";
        public string? IntegrationResponse { get; set; } = String.Empty;
        public string? IntegrationRequest { get; set; } = String.Empty;
    }
}

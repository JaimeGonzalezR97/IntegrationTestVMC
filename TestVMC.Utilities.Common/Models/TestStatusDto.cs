using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestVMC.Utilities.Common.Models
{
    public class TestStatusDto
    {
        public string ContactInformation { get; set; } = "Failed";
        public string VehicleInformation { get; set; } = "Failed";
        public string VehicleCondition { get; set; } = "Failed";
        public string VehicleDetails { get; set; } = "Failed";
        public string RulesAndIntegrations { get; set; } = "Failed";

    }
}

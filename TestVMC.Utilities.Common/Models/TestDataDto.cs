using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueMyCar.Application.DTO;

namespace TestVMC.Utilities.Common.Models
{
    public class TestDataDto
    {
        public List<TemporaryDatumDto>? ContactInformation { get; set; }
        public List<TemporaryDatumDto>? VehicleInformation { get; set; }
        public List<TemporaryDatumDto>? VehicleCondition { get; set; }
        public List<TemporaryDatumDto>? VehicleDetails { get; set; }
        public List<TemporaryDatumDto>? RulesAndIntegrations { get; set; }
    }
}

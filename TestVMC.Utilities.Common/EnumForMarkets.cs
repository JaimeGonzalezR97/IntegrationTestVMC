using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestVMC.Utilities.Common
{
    public class EnumForMarkets
    {

        public enum CountryBrandId 
        {
            AUSSUBAMEL = 2,
            AUSSUBA = 3,
            AUSBRV = 4

        }
        public enum AUSSUBA
        {
            RedBookCode = 48,
            RegistryNumber = 51,
            State = 52,
        }
        public enum AUSSUBAMEL
        {
            RedBookCode = 48,
            RegistryNumber = 5,
            State = 6,
        }

        public enum FormId
        {
            ContactInformation = 4,
            VehicleInformation = 1,
            VehicleCondition = 2,
            VehicleDetails = 3
        }
    }
}

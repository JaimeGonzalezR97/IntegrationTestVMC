using Newtonsoft.Json;
using AutoMapper;
using MathNet.Numerics.Financial;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Runtime.Intrinsics.Arm;
using TestVMC.Utilities.Common;
using ValueMyCar.Application.DTO;
using ValueMyCar.Application.Interface;
using ValueMyCar.Application.Main;
using ValueMyCar.Domain.Core;
using ValueMyCar.Domain.Entity;
using ValueMyCar.Domain.Interface;
using ValueMyCar.Infraestructure.Data;
using ValueMyCar.Infraestructure.Interface;
using ValueMyCar.Infraestructure.Repository;
using ValueMyCar.Services.ApiFields.Controllers;
using ValueMyCar.Services.ApiTemporaryData.Controllers;
using ValueMyCar.Transversal.Mapper;
using NPOI.POIFS.Crypt.Dsig;
using NPOI.SS.Formula.Functions;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using Bogus;
using ValueMyCar.Services.ApiVehicleInformation.Controllers;
using NuGet.Protocol;
using Microsoft.AspNetCore.Mvc;
using NuGet.Packaging.Signing;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ValueMyCar.Transversal.Common;
using Bogus.DataSets;
using System.Runtime.CompilerServices;
using ValueMyCar.Services.ApiBusinessRule.Controllers;
using Org.BouncyCastle.Asn1.Ocsp;
using static TestVMC.Utilities.Common.EnumForMarkets;

namespace TestVMC.Test.AustraliaSubaru
{
    public class Test
    {
        Faker _faker = new Faker();
        private DataDto _requireData = new();
        private CommonFunctions _commonFunctions = new();
        private string jsonData = "";
        private string abbreviation = "";
        private string identifier = "";
        private IMapper _mapper;

        public Test()
        {
            identifier = _faker.Random.AlphaNumeric(10);

        }

        [SetUp]
        public void SetUp()
        {
            var configuration = AppConfigurations.LoadConfiguration();
            abbreviation = "AUSSUBA";
            _mapper = AppConfigurations.MapperConfig();
            jsonData = File.ReadAllText("requiredFields.json");
            _requireData = JsonConvert.DeserializeObject<DataDto>(jsonData);
        }

        [Test, Order(1)]
        public async Task ContactInformation_OK()
        {
            //Arrange (preparacion)
            DataDto formData = new();
            ControllersConfig controllersConfig = new();
            List<TemporaryDatumDto> listTemporaryDatum = new();

            TemporaryDatumController temporaryController = 
                await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController = 
                await controllersConfig.GetController<FieldsController>(abbreviation);

            int formId = (int)FormId.ContactInformation;


            //Act(Acciones a ejecutar)
            var resultFields = await fieldsController.GetFields(formId, abbreviation);
            listTemporaryDatum = _commonFunctions.CompleteFields(resultFields.Data, _requireData);
            formData.Body = listTemporaryDatum;
            formData.Market = abbreviation;
            formData.Reject = false;
            formData.FormId = formId;
            formData.StatusForm = false;
            formData.Identifier = identifier;
            var resultTemporary = await temporaryController.CreateOrUpdate(formData);

            //Asserts
            Assert.IsTrue(resultTemporary.IsSuccess);
        }

        [Test, Order(2)]
        public async Task VehicleInformation_OK()
        {
            //Arrange
            int registryNumberId = (int)AUSSUBA.RegistryNumber;
            int stateId = (int)AUSSUBA.State;
            int redbookCodeId = (int)AUSSUBA.RedBookCode;
            int formId = (int)FormId.VehicleInformation;
            ControllersConfig controllersConfig = new();

            TemporaryDatumController temporaryController =
                await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController =
                await controllersConfig.GetController<FieldsController>(abbreviation);
            VehicleInformationController vehicleController =
                await controllersConfig.GetController<VehicleInformationController>(abbreviation);

            DataDto dataDto = new();
            VehicleInformationDto inputDto = new()
            {
                CountryBrandId = (int)CountryBrandId.AUSSUBA,
                FormId = formId,
                Body = new List<DetailInformationDto>()
                {
                    new DetailInformationDto(){ FieldId = registryNumberId, value = _requireData.Body.Find(x => x.FieldId == registryNumberId).Value},
                    new DetailInformationDto(){ FieldId = stateId, value = _requireData.Body.Find(x => x.FieldId == stateId).Value}
                }
            };

            //Action
            var resulFields = await fieldsController.GetFields(1, abbreviation);
            var resultVehicleInfo = await vehicleController.GetVehicleInformation(inputDto);
            if(resultVehicleInfo is OkObjectResult okResult)
            {
                var json = JsonConvert.SerializeObject(okResult.Value);
                Response<VehicleInformationDto> responseDto = JsonConvert.DeserializeObject<Response<VehicleInformationDto>>(json);
                var data = responseDto.Data.Body;
                var listTemporaryDatum = _mapper.Map<List<TemporaryDatumDto>>(responseDto.Data.Body);
                //Extraigo los campos que no esten activos
                var inactiveFields = resulFields.Data.Where(x => x.Active == false).ToList();
                var inactiveFieldsId = inactiveFields.Select(x => x.FieldId);
                //removemos los campos que no necesitan ser guardados
                listTemporaryDatum.RemoveAll(x => x.FieldId == 0 || inactiveFieldsId.Contains(x.FieldId));
                dataDto = new DataDto()
                {
                    Identifier = identifier,
                    Market = abbreviation,
                    FormId = formId,
                    StatusForm = false,
                    Reject = false,
                    Body = listTemporaryDatum
                };
            }
            else
            {
               
                var listTemporaryDatum = _commonFunctions.CompleteFields(resulFields.Data, _requireData);
                listTemporaryDatum.Add(
                    new TemporaryDatumDto() { FieldId = redbookCodeId, Value = _requireData.Body.Find(x => x.FieldId == redbookCodeId).Value });
                dataDto = new DataDto()
                {
                    Identifier = identifier,
                    Market = abbreviation,
                    FormId = formId,
                    StatusForm = false,
                    Reject = false,
                    Body = listTemporaryDatum
                };
            }

            var resultTemporary = await temporaryController.CreateOrUpdate(dataDto);

            //Assert
            Assert.That(resultTemporary.IsSuccess);

        }

        [Test, Order(3)]
        public async Task VehicleCondition_OK()
        {
            //Arrange
            ControllersConfig controllersConfig = new();

            TemporaryDatumController temporaryController =
                await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController = 
                await controllersConfig.GetController<FieldsController>(abbreviation);

            TemporaryDatumDto condition = new();

            //Actions
            var listFields = await fieldsController.GetFields(2, abbreviation);
            foreach(var field in listFields.Data)
            {
                var fieldCondition = _requireData.Body.Find(x => x.FieldId == field.FieldId);
                if (fieldCondition != null)
                {
                    condition = fieldCondition;
                }
            }

            DataDto dataDto = new()
            {
                FormId = (int)FormId.VehicleCondition,
                Identifier = identifier,
                Market = abbreviation,
                StatusForm = false,
                Reject = false,
                Body = new List<TemporaryDatumDto>()
                {
                    condition
                }
            };

            var resultTemporary = await temporaryController.CreateOrUpdate(dataDto);

            //Assert
            Assert.IsTrue(resultTemporary.IsSuccess);
        }

        [Test, Order(4)]
        public async Task VehicleDetails_OK()
        {
            //Arrange
            ControllersConfig controllersConfig = new();
            List<TemporaryDatumDto> listTemporaryDatum = new();

            TemporaryDatumController temporaryController =
                await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController =
                await controllersConfig.GetController<FieldsController>(abbreviation);
            VehicleInformationController vehicleController =
                await controllersConfig.GetController<VehicleInformationController>(abbreviation);
            int formId = (int)FormId.VehicleDetails;
            VehicleInformationDto _vehicleInformationDto = new()
                {
                CountryBrandId = (int)CountryBrandId.AUSSUBA,
                Identifier = identifier,
                FormId = formId,
                Body = new List<DetailInformationDto>()
                };


            //Actions
            var resultFields = await fieldsController.GetFields(formId, abbreviation);
            var resultVehicleInformation = await vehicleController.GetVehiclePrices(_vehicleInformationDto);
            listTemporaryDatum = _commonFunctions.CompleteFields(resultFields.Data, _requireData);
            var okResult = resultVehicleInformation as OkObjectResult;
            var json = JsonConvert.SerializeObject(okResult.Value);
            Response<VehicleInformationDto> responseDto = JsonConvert.DeserializeObject<Response<VehicleInformationDto>>(json);
            listTemporaryDatum = listTemporaryDatum.Concat(_mapper.Map<List<TemporaryDatumDto>>(responseDto.Data.Body)).ToList(); //juantamos las respuestas de los campos con los precios obtenidos
            DataDto dataDto = new DataDto()
            {
                Identifier = identifier,
                Market = abbreviation,
                FormId = formId,
                StatusForm = false,
                Reject = false,
                Body = listTemporaryDatum
            };
            var resultTemporaryDatum = await temporaryController.CreateOrUpdate(dataDto);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(resultTemporaryDatum.IsSuccess);
                Assert.That(responseDto.IsSuccess);
            });
        }

        [Test, Order(5)]
        public async Task RulesAndIntegration_OK()
        {
            //Arrange
            ControllersConfig controllersConfig = new();

            TemporaryDatumController temporaryController =
                await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            BusinessRuleController businessRuleController =
                await controllersConfig.GetController<BusinessRuleController>(abbreviation);

            //Actions
            DataDto dataDto = new()
            {
                Identifier = identifier,
                Market = abbreviation,
                FormId = (int)FormId.VehicleDetails,
                StatusForm = false,
                Reject = false,
                Body = new List<TemporaryDatumDto>()
            };

            var response = await _commonFunctions.ExecuteRules(dataDto);
            var responseDatum = await temporaryController.CreateOrUpdate(response.Data);
            await _commonFunctions.ExecuteIntegration(identifier, abbreviation);
            await _commonFunctions.ConsolePrint(response);
            //Asserts
            Assert.That(responseDatum.IsSuccess);
        }
    }
}
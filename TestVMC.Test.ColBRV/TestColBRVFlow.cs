using Newtonsoft.Json;
using AutoMapper;
using Bogus;
using Bogus.Bson;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NPOI.HSSF.Record.PivotTable;
using NuGet.ContentModel;
using System.Text.Json.Nodes;
using TestVMC.Utilities.Common;
using ValueMyCar.Application.DTO;
using ValueMyCar.Services.ApiBusinessRule.Controllers;
using ValueMyCar.Services.ApiFields.Controllers;
using ValueMyCar.Services.ApiTemporaryData.Controllers;
using ValueMyCar.Services.ApiVehicleInformation.Controllers;
using ValueMyCar.Transversal.Common;

namespace TestVMC.Test.ColBRV
{
    public class Tests
    {
        Faker faker = new Faker();
        private DataDto _requireData = new();
        private CommonFunctions _commonFunctions = new CommonFunctions();
        private string jsonData = "";
        private string abbreviation = "";
        private string identifier = "";
        private IMapper _mapper;

        public Tests()
        {
            identifier = faker.Random.AlphaNumeric(10);
        }

        [SetUp]
        public void Setup()
        {
            var configuration = AppConfigurations.LoadConfiguration();
            abbreviation = configuration.GetSection("ColombiaBravoauto:Abbreviation").Value;
            _mapper = AppConfigurations.MapperConfig();
            jsonData = File.ReadAllText("requiredFieldsColBRV.json");
            _requireData = JsonConvert.DeserializeObject<DataDto>(jsonData);
        }

        [Test, Order(1)]
        public async Task contactInfomation()
        {
            //Arrange
            DataDto formData = new();
            ControllersConfig controllersConfig = new();
            List<TemporaryDatumDto> listTemporaryDatum = new();

            TemporaryDatumController temporaryDatum = await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController = await controllersConfig.GetController<FieldsController>(abbreviation);
            int formid = 4;
            //Act
            var fields = await fieldsController.GetFields(formid, abbreviation);
            listTemporaryDatum = _commonFunctions.CompleteFields(fields.Data, _requireData);

            formData.Body = listTemporaryDatum;
            formData.Identifier = identifier;
            formData.Market = abbreviation;
            formData.FormId = formid;
            formData.StatusForm = false;
            formData.Reject = false;
            var resultDatum = await temporaryDatum.CreateOrUpdate(formData);


            //Assert
            Assert.IsTrue(resultDatum.IsSuccess);

        }
        [Test, Order(2)]
        public async Task vehicleInformation()
        {
            //Arrange
            ControllersConfig controlsConfig = new();
            DataDto formData = new();
            List<TemporaryDatumDto> temporaryData = new();
            TemporaryDatumController temporaryDatumController = await controlsConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController = await controlsConfig.GetController<FieldsController>(abbreviation);
            int formid = 1;
            //Act
            var fields = await fieldsController.GetFields(formid, abbreviation);
            temporaryData = _commonFunctions.CompleteFields(fields.Data, _requireData);

            formData.Identifier = identifier;
            formData.Market = abbreviation;
            formData.FormId = formid;
            formData.StatusForm = false;
            formData.Reject = false;
            formData.Body = temporaryData;
            var responseTemporaryDatum = await temporaryDatumController.CreateOrUpdate(formData);

            //Assert
            Assert.IsTrue(responseTemporaryDatum.IsSuccess);

        }
        [Test, Order(3)]
        public async Task vehicleCondition()
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
            foreach (var field in listFields.Data)
            {
                var fieldCondition = _requireData.Body.Find(x => x.FieldId == field.FieldId);
                if (fieldCondition != null)
                {
                    condition = fieldCondition;
                }
            }

            DataDto dataDto = new()
            {
                FormId = 2,
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
        public async Task vehicleDetails()
        {
            //Arrange

            ControllersConfig controlsConfig = new();
            DataDto formData = new();
            List<TemporaryDatumDto> temporaryDatum = new();

            FieldsController fieldsController = await controlsConfig.GetController<FieldsController>(abbreviation);
            TemporaryDatumController temporaryDatumController = await controlsConfig.GetController<TemporaryDatumController>(abbreviation);
            VehicleInformationController vehicleInformationController = await controlsConfig.GetController<VehicleInformationController>(abbreviation);
            int formId = 3;
            List<int> priceIds = new List<int>();
            priceIds.Add(48);
            priceIds.Add(158);

            List<DetailInformationDto> detailInformationDto = new List<DetailInformationDto>();
            VehicleInformationDto vehicleInformationDto = new()
            {
                FormId = formId,
                Identifier = identifier,
                CountryBrandId = 5,
                Body = detailInformationDto
            };
            //Act
            var fields = await fieldsController.GetFields(formId, abbreviation);
            temporaryDatum = _commonFunctions.CompleteFields(fields.Data, _requireData);

            foreach (int id in priceIds)
            {
                var infoRequiredFields = _requireData.Body.Find(x => x.FieldId == id);
                DetailInformationDto infoCar = new()
                {
                    FieldId = id,
                    value = infoRequiredFields.Value
                };
                detailInformationDto.Add(infoCar);

            }
            var vehiclePrice = await vehicleInformationController.GetVehiclePrices(vehicleInformationDto);
            OkObjectResult okResult = vehiclePrice as OkObjectResult;
            var jsonResult = JsonConvert.SerializeObject(okResult.Value);
            Response<VehicleInformationDto> responseDto = JsonConvert.DeserializeObject<Response<VehicleInformationDto>>(jsonResult);
            temporaryDatum = temporaryDatum.Concat(_mapper.Map<List<TemporaryDatumDto>>(responseDto.Data.Body)).ToList();
            formData.Identifier = identifier;
            formData.Market = abbreviation;
            formData.FormId = formId;
            formData.Reject = false;
            formData.StatusForm = false;
            formData.Body = temporaryDatum;


            var resultTemporaryDatum = await temporaryDatumController.CreateOrUpdate(formData);


            //Assert

            Assert.Multiple(() =>
            {
                Assert.That(resultTemporaryDatum.IsSuccess);
                Assert.That(responseDto.IsSuccess);

            });
        }

        [Test, Order(5)]
        public async Task rulesAndIntegrations()
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
                FormId = 3,
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
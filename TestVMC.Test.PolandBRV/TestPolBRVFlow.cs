
using Newtonsoft.Json;
using AutoMapper;
using Bogus;
using Bogus.Bson;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TestVMC.Utilities.Common;
using ValueMyCar.Application.DTO;
using ValueMyCar.Services.ApiBusinessRule.Controllers;
using ValueMyCar.Services.ApiFields.Controllers;
using ValueMyCar.Services.ApiTemporaryData.Controllers;
using ValueMyCar.Services.ApiVehicleInformation.Controllers;
using ValueMyCar.Transversal.Common;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using ValueMyCar.Domain.Entity;

namespace TestVMC.Test.PolandBRV
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
            abbreviation = configuration.GetSection("PoloniaBravoauto:Abbreviation").Value;
            _mapper = AppConfigurations.MapperConfig();
            jsonData = File.ReadAllText("requiredFieldsPol.json");
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
            DataDto formData = new();
            ControllersConfig controllersConfig = new();
            List<TemporaryDatumDto> listTemporaryDatum = new();
            var urlIndicata = "https://ws.indicata.com/vivi/v2/PL";
            TemporaryDatumController temporaryDatumController = await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            FieldsController fieldsController = await controllersConfig.GetController<FieldsController>(abbreviation);
            int formId = 1;
            CredentialsDto credentials = new CredentialsDto
            {
                Username = "jelena.avanesova@inchcape.lv",
                Password = "Indi2023!!",
                Profile = "MAX_PURCHASE_PRICE_100"
            };
            string href = "";
            string substringHref = "";

            //Act
            using (HttpClient httpClient = new HttpClient())
            {

                string authInfo = $"{credentials.Username}:{credentials.Password}";
                string base64AuthInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64AuthInfo);

                HttpResponseMessage response = await httpClient.GetAsync(urlIndicata);

                string reponseBody = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(reponseBody);
                href = (string)json["nextStep"][1]["href"];
                int indexV2 = href.IndexOf("v2/");
                indexV2 += 3;
                int indexPl = href.IndexOf("/PL", indexV2);
                substringHref = href.Substring(indexV2, indexPl - indexV2);

            }
            var indicataURLBase = _requireData.Body.Find(x => x.FieldId == 206).Value;
            indicataURLBase = indicataURLBase.Replace("{token}", substringHref);
            var fields = await fieldsController.GetFields(formId, abbreviation);
            listTemporaryDatum = _commonFunctions.CompleteFields(fields.Data, _requireData);
            TemporaryDatumDto newTempData = new TemporaryDatumDto
            {
                Value = indicataURLBase,
                FieldId = 206,
                Operator = "",
                Action = "",
                MessageSf = ""

            };
            listTemporaryDatum.Add(newTempData);


            formData.Body = listTemporaryDatum;
            formData.Identifier = identifier;
            formData.Market = abbreviation;
            formData.FormId = formId;
            formData.StatusForm = false;
            formData.Reject = false;

            var createTempData = await temporaryDatumController.CreateOrUpdate(formData);

            //Assert
            Assert.IsTrue(createTempData.IsSuccess);
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
            ControllersConfig controllersConfig = new();
            DataDto formData = new();
            List<TemporaryDatumDto> temporaryData = new();
            FieldsController fieldsController = await controllersConfig.GetController<FieldsController>(abbreviation);
            TemporaryDatumController temporaryDatumController = await controllersConfig.GetController<TemporaryDatumController>(abbreviation);
            int formId = 3;
            VehicleInformationController vehicleInformationController = await controllersConfig.GetController<VehicleInformationController>(abbreviation);
            List<int> priceIds = new List<int>();
            priceIds.Add(203);
            //priceIds.Add(201);

            List<DetailInformationDto> detailInformationDto = new List<DetailInformationDto>();
            VehicleInformationDto vehicleInformationDto = new()
            {
                FormId = formId,
                Identifier = identifier,
                CountryBrandId = 6,
                Body = detailInformationDto
            };
            //Act

            var fields = await fieldsController.GetFields(formId, abbreviation);
            temporaryData = _commonFunctions.CompleteFields(fields.Data, _requireData);

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
            temporaryData = temporaryData.Concat(_mapper.Map<List<TemporaryDatumDto>>(responseDto.Data.Body)).ToList();
            formData.Identifier = identifier;
            formData.Market = abbreviation;
            formData.FormId = formId;
            formData.Reject = false;
            formData.StatusForm = false;
            formData.Body = temporaryData;


            var resultTemporaryDatum = await temporaryDatumController.CreateOrUpdate(formData);

            //Assert
            Assert.That(resultTemporaryDatum.IsSuccess);
            Assert.That(responseDto.IsSuccess);
        }
        [Test,Order(5)]
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
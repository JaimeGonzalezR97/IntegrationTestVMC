using AutoMapper;
using Bogus;
using System.Text.Json.Nodes;
using TestVMC.Utilities.Common;
using ValueMyCar.Application.DTO;

namespace TestVMC.Test.ChileBRV

{
    public class Tests
    {
        Faker faker = new Faker();
        private DataDto _dataDto = new();
        private CommonFunctions _commonFunctions = new CommonFunctions();
        private string jsonData = "";
        private string abbreviation = "";
        private string identifier = "";
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            identifier = faker.Random.AlphaNumeric(10);
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}
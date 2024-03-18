using AutoMapper;
using Bogus;
using Bogus.DataSets;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestVMC.Utilities.Common.Models;
using ValueMyCar.Application.DTO;
using ValueMyCar.Application.Interface;
using ValueMyCar.Application.Main;
using ValueMyCar.Domain.Core;
using ValueMyCar.Domain.Interface;
using ValueMyCar.Infraestructure.Data;
using ValueMyCar.Infraestructure.Interface;
using ValueMyCar.Infraestructure.Repository;
using ValueMyCar.Transversal.Common;

namespace TestVMC.Utilities.Common
{
    public class CommonFunctions
    {
        private IMapper _mapper;
        Faker faker = new Faker();

        private readonly ValueMyCarContext _context;

        //External integrations for ApitemporaryDatum
        private readonly IIntegrationSystemApplication _integrationApplication;
        private readonly IIntegrationContextApplication _integrationContext;
        private readonly IIntegrationSystemDomain _integrationDomain;
        private readonly IIntegrationSystemRepository _integrationRepository;

        public CommonFunctions()
        {
            _mapper = AppConfigurations.MapperConfig();
            _context = new ValueMyCarContext(AppConfigurations.ContextConnection());
            _integrationRepository = new IntegrationSystemRepository(_context);
            _integrationDomain = new IntegrationSystemDomain(_integrationRepository);
            _integrationContext = new IntegrationContextApplication();
            _integrationApplication = new IntegrationSystemApplication(_integrationContext, _integrationDomain, AppConfigurations.MapperConfig());
        }

        public List<TemporaryDatumDto> CompleteFields(IEnumerable<TotalFieldsFinalDTO> fields, DataDto requireData)
        {
            List<TemporaryDatumDto> listTemporaryDatum = new();

            listTemporaryDatum = _mapper.Map<List<TemporaryDatumDto>>(fields);
            listTemporaryDatum.RemoveAll(x => x.FieldId == 0);
            listTemporaryDatum.ForEach(x =>
            {
                var result = requireData.Body.Find(y => y.FieldId == x.FieldId);
                if (result != null)
                {
                    x.Value = result.Value;
                }
                else
                {
                    x.Value = faker.Lorem.Sentence(3);
                }

            }
            );
            return listTemporaryDatum;
        }

        public async Task<Response<DataDto>> ExecuteRules(DataDto dataDto)
        {
            string baseUrl = "https://localhost:7065/api/BusinessRule/ValidateRejection";
            string json = JsonConvert.SerializeObject(dataDto);

            using (HttpClient client = new HttpClient())
            {

                HttpResponseMessage response = await client.PostAsync(baseUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    Response<DataDto> dataResponse = JsonConvert.DeserializeObject<Response<DataDto>>(content);
                    return dataResponse;
                }
                else
                {
                    return new Response<DataDto>()
                    {
                       IsSuccess = false,
                        Message = "check if the service is up or some rule is overlapping another",
                        Data = new DataDto() {
                            Body = dataDto.Body,
                            Reject = true },
                    };
                }


            }

        }

        public async Task ExecuteIntegration(string abbreviation, string identifier)
        {
            _integrationApplication.IntegrationsAsync(abbreviation, identifier).Wait();
        }

        public async Task CreateTestLog(string testName,bool rejectStatus ,TestDataDto testDataDto, TestStatusDto testStatusDto)
        {
            await CreateTableIfNotExists();

            TestDto testDto = new TestDto()
            {
                TestName = testName,
                DateTime = DateTime.Now,
                RejectStatus = rejectStatus,
                TestStatus = JsonConvert.SerializeObject(testStatusDto),
                TestData = JsonConvert.SerializeObject(testDataDto)

            };

            if (!rejectStatus)
            {
                var query = await _context.TransactionalLogs.OrderByDescending(x => x.LogId).FirstOrDefaultAsync();
                testDto.IntegrationSatatus = query.Status;
                testDto.IntegrationResponse = query.Response;
                testDto.IntegrationRequest = query.Request;
            }
            await InsertDataInTestTable(testDto);

        }

        public async Task ConsolePrint(Response<DataDto> responseDataDto)
        {
            var query = await _context.TransactionalLogs.OrderByDescending(x => x.LogId).FirstOrDefaultAsync();

            string filePath = "log.txt";

            string content = $"Date: {query.Date}" +
                $"\n\nData form:\n{JsonConvert.SerializeObject(responseDataDto)}" +
                $"\n\nIntegration status:{query.Status}" +
                $"\n\nIntegration response:\n{query.Response}" +
                $"\n\nIntegration request:\n{query.Request}";

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            File.WriteAllText(filePath, content);

        }

        private async Task CreateTableIfNotExists()
        {
            string createQuery = "CREATE TABLE [dbo].[Test]([test_id][int] IDENTITY(1, 1) NOT NULL, [test_name] [varchar] (60) NOT NULL, [date] [datetime] NOT NULL, [test_status] [varchar] (500) NOT NULL, [temporary_data] [varchar] (MAX) NOT NULL,[reject][bit] not null,[integration_status][varchar] (20) NOT NULL, [integration_response] [varchar] (MAX) NULL,[integration_request][varchar] (MAX) NULL,CONSTRAINT[PK_Test] PRIMARY KEY CLUSTERED([test_id] ASC)WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON[PRIMARY]) ON[PRIMARY]";
            string sqlQuery = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Test') {createQuery}";
            await _context.Database.ExecuteSqlRawAsync(sqlQuery);
        }

        private async Task InsertDataInTestTable(TestDto testDto)
        {
            try
            {
                string sqlQuery = "INSERT INTO Test (test_name,date,reject,test_status,temporary_data,integration_status,integration_response,integration_request) VALUES (@testname,@date,@reject,@teststatus,@temporarydata,@integrationstatus,@integrationresponse,@integrationrequest)";

                SqlParameter[] parameters = new SqlParameter[]
                {
                new SqlParameter("@testname",testDto.TestName),
                new SqlParameter("@date", testDto.DateTime),
                new SqlParameter("@reject",testDto.RejectStatus),
                new SqlParameter("@teststatus",testDto.TestStatus),
                new SqlParameter("@temporarydata",testDto.TestData),
                new SqlParameter("@integrationstatus",testDto.IntegrationSatatus),
                new SqlParameter("@integrationresponse",testDto.IntegrationResponse),
                new SqlParameter("@integrationrequest",testDto.IntegrationRequest)
                };

                await _context.Database.ExecuteSqlRawAsync(sqlQuery, parameters);
            }
            catch(Exception ex) 
            {
                throw;
            }
            
        }
    }
}

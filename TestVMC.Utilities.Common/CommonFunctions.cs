using AutoMapper;
using Bogus;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                try
                {
                    HttpResponseMessage response = await client.PostAsync(baseUrl, new StringContent(json, Encoding.UTF8, "application/json"));
                    if(response.IsSuccessStatusCode) 
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        Response<DataDto> dataResponse = JsonConvert.DeserializeObject<Response<DataDto>>(content);
                        return dataResponse;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
            }

        }

        public async Task ExecuteIntegration(string abbreviation, string identifier)
        {
            _integrationApplication.IntegrationsAsync(abbreviation, identifier).Wait();
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
    }
}

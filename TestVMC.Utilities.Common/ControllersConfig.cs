using AutoMapper;
using Bogus;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValueMyCar.Application.DTO;
using ValueMyCar.Application.Interface;
using ValueMyCar.Application.Main;
using ValueMyCar.Domain.Core;
using ValueMyCar.Domain.Entity;
using ValueMyCar.Domain.Interface;
using ValueMyCar.Infraestructure.Data;
using ValueMyCar.Infraestructure.Interface;
using ValueMyCar.Infraestructure.Repository;
using ValueMyCar.Services.ApiBusinessRule.Controllers;
using ValueMyCar.Services.ApiFields.Controllers;
using ValueMyCar.Services.ApiPriceEngine.Controllers;
using ValueMyCar.Services.ApiTemporaryData.Controllers;
using ValueMyCar.Services.ApiVehicleInformation.Controllers;

namespace TestVMC.Utilities.Common
{

    public class ControllersConfig
    {
        //ApiFields
        private readonly FieldsController _fieldsController;
        private readonly IFieldApplication _fieldsApplication;
        private readonly IFieldsDomain _fieldsDomain;
        private readonly IFieldsRepository _fieldsRepository;

        //ApiTemporaryDatum
        private readonly TemporaryDatumController _datumController;
        private readonly ITemporaryDatumApplication _datumApplication;
        private readonly ITemporaryDataDomain _datumDomain;
        private readonly ITemporaryDataRepository _datumRepository;
        private readonly IActionTemporaryDatum _actionDatum;

        //External integrations for ApitemporaryDatum
        private readonly IIntegrationSystemApplication _integrationApplication;
        private readonly IIntegrationContextApplication _integrationContext;
        private readonly IIntegrationSystemDomain _integrationDomain;
        private readonly IIntegrationSystemRepository _integrationRepository;

        //ApiVehicleInformation
        private IVehicleInformationApplication _vehicleInfoApplication;
        private IVehicleContextApplication _vehicleContextApplication;
        private readonly IVehicleInformationDomain _vehicleInfoDomain;
        private readonly IVehicleInformationRepository _vehicleinfoRepository;

        //Context
        private readonly ValueMyCarContext _context;

        public ControllersConfig()
        {
            //context
            _context = new ValueMyCarContext(AppConfigurations.ContextConnection());

            //apiFields
            _fieldsRepository = new FieldsRepository(_context);
            _fieldsDomain = new FieldsDomain(_fieldsRepository);
            _fieldsApplication = new FieldsApplication(_fieldsDomain, AppConfigurations.MapperConfig());
            _fieldsController = new FieldsController(_fieldsApplication);

            //apiTemporaryDatum
            _datumRepository = new TemporaryDataRepository(_context);
            _datumDomain = new TemporaryDataDomain(_datumRepository);
            _actionDatum = new ActionTemporaryDatum(_datumDomain, AppConfigurations.MapperConfig());
            _datumApplication = new TemporaryDatumApplication(AppConfigurations.MapperConfig(), _actionDatum);
            _integrationRepository = new IntegrationSystemRepository(_context);
            _integrationDomain = new IntegrationSystemDomain(_integrationRepository);
            _integrationContext = new IntegrationContextApplication();
            _integrationApplication = new IntegrationSystemApplication(_integrationContext, _integrationDomain, AppConfigurations.MapperConfig());
            _datumController = new TemporaryDatumController(_datumApplication, _integrationApplication);

            //apiVehicleInformation
            _vehicleinfoRepository = new VehicleInformationRepository(_context);
            _vehicleInfoDomain = new VehicleInformationDomain(_vehicleinfoRepository);
        }

        //public CommonFunctions

        public async Task<T> GetController<T>(string? abbreviation)
        {

            var countryBrand = await _integrationRepository.GetCountryBrandByMarketAsync(abbreviation);
            Dictionary<string,object> dictionary = new Dictionary<string, object>
            {
                { "FieldsController", _fieldsController},
                { "TemporaryDatumController", _datumController },
                { "VehicleInformationController", new VehicleInformationController(GetVehicleInfoStrategy<IVehicleInformationApplication>(countryBrand.CountryBrandId)) }
            };

            string controllerName = typeof(T).Name;

            if(dictionary.TryGetValue(controllerName, out object controller))
            {
                return (T)controller;
            }
            else
            {
                return default(T);
            }
        }

        private T GetVehicleInfoStrategy<T>(int countryBrandId)
        {
            Dictionary<int, object> dictionary = new Dictionary<int, object>
            {
                { 2, new RedBookStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig()) },
                { 3, new RedBookStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig()) },
                { 4, new RedBookStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig()) },
                { 5, new AutoFactStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig())},
                { 6, new IndicataStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig()) },
                { 7, new AutoFactChileStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig())},
                { 8, new AutoFactPeruStrategyApplication(_vehicleInfoDomain, AppConfigurations.MapperConfig())}
            };

            if(dictionary.TryGetValue(countryBrandId, out object strategy))
            {
                _vehicleContextApplication = new VehicleContextApplication((IVehicleInformationStrategy)strategy, _vehicleInfoDomain, AppConfigurations.MapperConfig());
                _vehicleInfoApplication = new VehicleInformationApplication(_vehicleContextApplication);
                return (T)_vehicleInfoApplication;
            }
            else
            {
                return default;
            }
        }

        public async Task ExecuteIntegration(string abbreviation, string identifier)
        {
            _integrationApplication.IntegrationsAsync(abbreviation, identifier).Wait();
        }
    }
}

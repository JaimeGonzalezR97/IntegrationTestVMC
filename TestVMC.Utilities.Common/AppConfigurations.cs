using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ValueMyCar.Application.DTO;
using ValueMyCar.Domain.Entity;
using ValueMyCar.Infraestructure.Data;
using ValueMyCar.Transversal.Mapper;

namespace TestVMC.Utilities.Common
{
    public class AppConfigurations
    {

        public static IConfigurationRoot LoadConfiguration()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(),"..","..", "..", "appsettings.test.json");
            var builder = new ConfigurationBuilder()
                .AddJsonFile(path);
            var configuration = builder.Build();
            return configuration;
        }
        public static DbContextOptions<ValueMyCarContext> ContextConnection()
        {
            var options = new DbContextOptionsBuilder<ValueMyCarContext>()
                .UseSqlServer(LoadConfiguration().GetSection("ConnectionStrings:ValueMyCar").Value)
                .Options;
            return options;
        }

        public static IMapper MapperConfig()
        {
            var configuracion = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
                cfg.CreateMap<TotalFieldsFinalDTO, TemporaryDatumDto>()
                .ForMember(dest => dest.FieldId, opt =>
                {
                    opt.Condition(src => (src.FieldType == 1 || src.FieldType == 2) && src.Active == true);
                    opt.MapFrom(src => src.FieldId);
                })
                .ReverseMap();
                cfg.CreateMap<TemporaryDatumDto, DetailInformationDto>().ReverseMap();
            });

            var mapper = configuracion.CreateMapper();
            return mapper;
        }

    }
}

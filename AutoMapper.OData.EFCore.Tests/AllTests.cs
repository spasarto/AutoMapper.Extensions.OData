﻿using AutoMapper.AspNet.OData;
using DAL.EFCore;
using Domain.OData;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AutoMapper.OData.EFCore.Tests
{
    public class AllTests
    {
        public AllTests()
        {
            Initialize();
        }

        #region Fields
        private IServiceProvider serviceProvider;
        #endregion Fields

        private void Initialize()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddOData();
            services.AddDbContext<MyDbContext>
                (
                    options =>
                    {
                        options.UseInMemoryDatabase("MyDbContext");
                        options.UseInternalServiceProvider(new ServiceCollection().AddEntityFrameworkInMemoryDatabase().BuildServiceProvider());
                    },
                    ServiceLifetime.Transient
                )
                .AddSingleton<AutoMapper.IConfigurationProvider>(new MapperConfiguration(cfg => cfg.AddMaps(typeof(AllTests).Assembly)))
                .AddTransient<IMapper>(sp => new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService))
                .AddTransient<IApplicationBuilder>(sp => new Microsoft.AspNetCore.Builder.Internal.ApplicationBuilder(sp))
                .AddTransient<IRouteBuilder>(sp => new RouteBuilder(sp.GetRequiredService<IApplicationBuilder>()));

            serviceProvider = services.BuildServiceProvider();

            MyDbContext context = serviceProvider.GetRequiredService<MyDbContext>();
            context.Database.EnsureCreated();
            Seed_Database(context);
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_filter_eq_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$filter=Name eq 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_filter_ne_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$filter=Name ne 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_filter_eq_no_expand()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$filter=Name eq 'One'"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 0);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_no_filter_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_no_expand_no_filter_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Buildings.Count == 0);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void OpsTenant_no_expand_filter_eq_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$filter=Name eq 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 0);
                Assert.True(collection.First().Name == "One");
            }
        }

        [Fact]
        public async void OpsTenant_expand_Buildings_expand_Builder_expand_City_filter_ne_and_order_by()
        {
            Test(await Get<OpsTenant, TMandator>("/opstenant?$top=5&$expand=Buildings($expand=Builder($expand=City))&$filter=Name ne 'One'&$orderby=Name desc"));

            void Test(ICollection<OpsTenant> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Buildings.Count == 2);
                Assert.True(collection.First().Buildings.First().Builder != null);
                Assert.True(collection.First().Buildings.First().Builder.City != null);
                Assert.True(collection.First().Name == "Two");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_filter_eq_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder,Tenant&$filter=name eq 'One L1'"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.Name == "Sam");
                Assert.True(collection.First().Tenant.Name == "One");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_filter_on_nested_property_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder,Tenant&$filter=Builder/Name eq 'Sam'&$orderby=Name asc"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 2);
                Assert.True(collection.First().Builder.Name == "Sam");
                Assert.True(collection.First().Tenant.Name == "One");
                Assert.True(collection.First().Name == "One L1");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_filter_on_property_and_order_by()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$filter=Name ne 'One L2'&$orderby=Name desc"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 3);
                Assert.True(collection.First().Builder.City != null);
                Assert.True(collection.First().Name != "One L2");
            }
        }

        [Fact]
        public async void Building_expand_Builder_Tenant_expand_City_filter_on_nested_nested_property()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding?$top=5&$expand=Builder($expand=City),Tenant&$filter=Builder/City/Name eq 'Leeds'"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.True(collection.Count == 1);
                Assert.True(collection.First().Builder.City.Name == "Leeds");
                Assert.True(collection.First().Name == "Two L2");
            }
        }

        [Fact]
        public async void Building_with_parameters_are_mapped()
        {
            string parameterValue = Guid.NewGuid().ToString();
            var parameters = new Dictionary<string, object>
            {
                { "parameter", parameterValue}
            };
            Test
            (
                await Get<CoreBuilding, TBuilding>
                (
                    "/corebuilding",
                    opts => parameters.Keys
                            .ToList()
                            .ForEach(key => opts.Items[key] = parameters[key])
                )
            );

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.Same(parameterValue, collection.First().Parameter);
            }
        }

        [Fact]
        public async void Building_without_parameters_arent_mapped()
        {
            Test(await Get<CoreBuilding, TBuilding>("/corebuilding"));

            void Test(ICollection<CoreBuilding> collection)
            {
                Assert.Same("unknown", collection.First().Parameter);
            }
        }

        private async Task<ICollection<TModel>> Get<TModel, TData>(string query, Action<IMappingOperationOptions<IEnumerable<TData>, IEnumerable<TModel>>> opts = null) where TModel : class where TData : class
        {
            return await DoGet
            (
                serviceProvider.GetRequiredService<IMapper>(),
                serviceProvider.GetRequiredService<MyDbContext>()
            );

            async Task<ICollection<TModel>> DoGet(IMapper mapper, MyDbContext context)
            {
                return await context.Set<TData>().GetAsync
                (
                    mapper,
                    ODataHelpers.GetODataQueryOptions<TModel>
                    (
                        query,
                        serviceProvider,
                        serviceProvider.GetRequiredService<IRouteBuilder>()
                    ),
                    HandleNullPropagationOption.False,
                    opts
                );
            }
        }

        static void Seed_Database(MyDbContext context)
        {
            context.City.Add(new TCity { Name = "London" });
            context.City.Add(new TCity { Name = "Leeds" });
            context.SaveChanges();

            List<TCity> cities = context.City.ToList();
            context.Builder.Add(new TBuilder { Name = "Sam", CityId = cities.First(b => b.Name == "London").Id });
            context.Builder.Add(new TBuilder { Name = "John", CityId = cities.First(b => b.Name == "London").Id });
            context.Builder.Add(new TBuilder { Name = "Mark", CityId = cities.First(b => b.Name == "Leeds").Id });
            context.SaveChanges();

            List<TBuilder> builders = context.Builder.ToList();
            context.MandatorSet.Add(new TMandator
            {
                Identity = Guid.NewGuid(),
                Name = "One",
                Buildings = new List<TBuilding>
                {
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "One L1", BuilderId = builders.First(b => b.Name == "Sam").Id },
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "One L2", BuilderId = builders.First(b => b.Name == "Sam").Id  }
                }
            });
            context.MandatorSet.Add(new TMandator
            {
                Identity = Guid.NewGuid(),
                Name = "Two",
                Buildings = new List<TBuilding>
                {
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "Two L1", BuilderId = builders.First(b => b.Name == "John").Id  },
                    new TBuilding { Identity =  Guid.NewGuid(), LongName = "Two L2", BuilderId = builders.First(b => b.Name == "Mark").Id  }
                }
            });
            context.SaveChanges();
        }
    }

    public static class ODataHelpers
    {
        public static ODataQueryOptions<T> GetODataQueryOptions<T>(string queryString, IServiceProvider serviceProvider, IRouteBuilder routeBuilder) where T : class
        {
            ODataConventionModelBuilder builder = new ODataConventionModelBuilder(serviceProvider);

            builder.EntitySet<T>(typeof(T).Name);
            IEdmModel model = builder.GetEdmModel();
            IEdmEntitySet entitySet = model.EntityContainer.FindEntitySet(typeof(T).Name);
            ODataPath path = new ODataPath(new Microsoft.OData.UriParser.EntitySetSegment(entitySet));

            routeBuilder.EnableDependencyInjection();

            Uri uri = new Uri(BASEADDRES + queryString);

            return new ODataQueryOptions<T>
            (
                new ODataQueryContext(model, typeof(T), path),
                new DefaultHttpRequest(new DefaultHttpContext() { RequestServices = serviceProvider })
                {
                    Method = "GET",
                    Host = new HostString(uri.Host, uri.Port),
                    Path = uri.LocalPath,
                    QueryString = new QueryString(uri.Query)
                }
            );

        }

        static readonly string BASEADDRES = "http://localhost:16324";
    }
}

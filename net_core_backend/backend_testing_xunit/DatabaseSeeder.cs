using net_core_backend.Models;
using net_core_backend.Context;
using Newtonsoft.Json;
using System.Security.Principal;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_testing_xunit
{
    public abstract class DatabaseSeeder
    {
        static protected DefaultModel[] example { get; private set; }


        protected IHttpContextAccessor http;
        protected ControllerContext controllerContext;
        protected readonly IContextFactory factory;


        protected DatabaseSeeder(IHttpContextAccessor http, IContextFactory factory)
        {
            this.factory = factory;
            this.http = http;

            //Seed(factory);
        }

        protected virtual void CreateIdentity(string auth)
        {
            // Configure identity
            var identity = new GenericIdentity(auth, ClaimTypes.NameIdentifier);
            var contextUser = new ClaimsPrincipal(identity); //add claims as needed
            var httpContext = new DefaultHttpContext()
            {
                User = contextUser
            };

            controllerContext = new ControllerContext()
            {
                HttpContext = httpContext,
            };

            http.HttpContext = httpContext;
        }


        public static void Seed(IContextFactory factory)
        {
            using (var a = factory.CreateDbContext())
            {
                // Re-creates database
                a.Database.EnsureDeleted();
                a.Database.EnsureCreated();


                // Seeds users
                example = new DefaultModel[1]
                {
                new DefaultModel() {Id = 1},
                };

                a.SaveChanges();
            }
        }


        protected string Serialize(object entity)
        {
            return JsonConvert.SerializeObject(entity, new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            });
        }
    }
}

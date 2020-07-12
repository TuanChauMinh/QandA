using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DbUp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QandA.Data;
using QandA.Hubs;

namespace QandA
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");

            EnsureDatabase.For.SqlDatabase(connectionString);

            var upgrader = DeployChanges.To.SqlDatabase(connectionString, null).WithScriptsEmbeddedInAssembly(System.Reflection.Assembly.GetExecutingAssembly()).WithTransaction().Build();

            if(upgrader.IsUpgradeRequired())
            {
                upgrader.PerformUpgrade();
            }
            services.AddControllers();

            /** The AddScoped method means that only one instance of the DataRepository class is created
            in a given HTTP request. 
            This means the lifetime of the class that is created lasts for the
            whole HTTP request.

            So, if ASP.NET Core encounters a second constructor that
            references IDataRepository in the same HTTP request, it will use the
            nstance of the DataRepository class it created previously.

            */
            services.AddScoped<IDataRepository, DataRepository>();

            services.AddCors(options => options.AddPolicy("CorsPolicy", builder => 
            builder.AllowAnyMethod()
            .AllowAnyHeader()
            .WithOrigins("http://localhost:3000")
            .AllowCredentials()));

            services.AddSignalR();
            services.AddMemoryCache();
            services.AddSingleton<IQuestionCache, QuestionCache>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors("CorsPolicy");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
            }


            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<QuestionsHub>("/questionshub");
            });
        }
    }
}

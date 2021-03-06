﻿using College.ApplicationCore.Constants;
using College.ApplicationCore.Interfaces;
using College.GrpcServer.Services;
using College.Microservice.BAL;
using College.ServerBLL;
using College.ServerDAL;
using College.Services.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace College.Microservice
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
            services.AddGrpc();

            services.AddControllers();

            // ***************** For REST End Points ***************** 
            // Adding EF Core
            var connectionString = Configuration[Constants.DataStore.SqlConnectionString];
            // services.AddDbContext<CollegeDbContext>(o => o.UseSqlServer(connectionString));
            services.AddDbContext<Persistence.CollegeDbContext>(options =>
            {
                options.UseSqlServer(connectionString,
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 10,
                    maxRetryDelay: TimeSpan.FromSeconds(60),
                    errorNumbersToAdd: null);
                });
            });
            
            services.AddScoped<ProfessorsBal>();
            services.AddScoped<ProfessorsDal>();
            // ***************** For REST End Points ***************** 

            // ***************** For gRPC End Points ***************** 
            // Server DAL
            services.AddDbContext<ServerDAL.Persistence.CollegeDbContext>(o => o.UseSqlServer(connectionString));

            // College Application Services
            services.AddScoped<IProfessorBLL, ProfessorBLL>();
            services.AddScoped<IProfessorDAL, ProfessorDAL>();

            // Adding Redis Cache 
            services.AddStackExchangeRedisCache(option =>
            {
                option.Configuration = Configuration[Constants.DataStore.RedisConnectionString];
                option.InstanceName = Constants.RedisCacheStore.InstanceName;
            });
            // ***************** For gRPC End Points ***************** 
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<CollegeGrpcService>();

                endpoints.MapControllers();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });

            });
        }
    }
}

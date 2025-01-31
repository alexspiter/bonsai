﻿using System;
using System.Globalization;
using Bonsai.Code.Services;
using Bonsai.Code.Services.Config;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Bonsai.Code.Config
{
    [UsedImplicitly]
    public partial class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build().Get<StaticConfig>();
            Environment = env;
        }

        private StaticConfig Configuration { get; }
        private IWebHostEnvironment Environment { get; }

        /// <summary>
        /// Registers all required services in the dependency injection container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
            AppContext.SetSwitch("System.Drawing.EnableUnixSupport", true);

            // order is crucial
            ConfigureMvcServices(services);
            ConfigureDatabaseServices(services);
            ConfigureAuthServices(services);
            ConfigureSearchServices(services);
            ConfigureAutomapper(services);
            ConfigureAppServices(services);
        }

        /// <summary>
        /// Configures the web framework pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            var startupService = app.ApplicationServices.GetService<StartupService>();

            if (Environment.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseSerilogRequestLogging();
            }

            if (Configuration.WebServer.RequireHttps)
                app.UseHttpsRedirection();

            if (Configuration.Debug.DetailedExceptions)
                app.UseDeveloperExceptionPage();
            
            ValidateAutomapperConfig(app);
            InitDatabase(app);
            
            app.UseForwardedHeaders(GetforwardedHeadersOptions())
               .UseStatusCodePagesWithReExecute("/error/{0}")
               .UseStaticFiles()
               .Use(startupService.RenderLoadingPage)
               .UseRouting()
               .UseAuthentication()
               .UseAuthorization()
               .UseSession()
               .UseRequestLocalization(GetRequestLocalizationOptions())
               .UseCookiePolicy()
               .UseEndpoints(x =>
               {
                   x.MapAreaControllerRoute("admin", "Admin", "admin/{controller}/{action}/{id?}");
                   x.MapControllers();
               });
        }

        /// <summary>
        /// Configures the options for header forwarding.
        /// </summary>
        private ForwardedHeadersOptions GetforwardedHeadersOptions()
        {
            var opts = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All,
            };
            opts.KnownProxies.Clear();
            opts.KnownNetworks.Clear();
            return opts;
        }

        /// <summary>
        /// Configures the options for request localization.
        /// </summary>
        private RequestLocalizationOptions GetRequestLocalizationOptions()
        {
            var culture = CultureInfo.GetCultureInfo("ru-RU");
            return new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(culture),
                SupportedCultures = new[] {culture},
                SupportedUICultures = new[] {culture}
            };
        }
    }
}

﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace Microsoft.NetCore
{
    // ref: https://github.com/aspnet/AspNetCore/blob/4e44025a52e4b73aa17e09a8041b0e166e0c5ce0/src/DefaultBuilder/src/WebHost.cs
    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IHost"/> and <see cref="IHostBuilder"/> with pre-configured defaults.
    /// </summary>
    public static class GenericHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)"/>,
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the NETCORE_ENVIRONMENT,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     and configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        /// </remarks>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder() =>
            CreateDefaultBuilder(args: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)"/>,
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the NETCORE_ENVIRONMENT,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     and configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        /// </remarks>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder(string[] args) =>
            CreateDefaultBuilder(args, "");

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)"/>,
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the parameter of <see cref="hostEnvironmentVariable"/>,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     load <see cref="IConfiguration"/> from supplied command line args,
        ///     and configure the <see cref="ILoggerFactory"/> to log to the console and debug output,
        /// </remarks>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder(string[] args, string hostEnvironmentVariable)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ConfigureContextDefault(builder, hostEnvironmentVariable);
            ConfigureConfigDefault(builder, args);
            ConfigureLoggingDefault(builder);

            return builder;
        }

        public static void ConfigureContextDefault(IHostBuilder builder, string contextEnvironmentVariable)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                env.ApplicationName = Assembly.GetExecutingAssembly().GetName().Name;
                if (string.IsNullOrWhiteSpace(contextEnvironmentVariable))
                {
                    contextEnvironmentVariable = "NETCORE_ENVIRONMENT";
                }
                env.EnvironmentName = System.Environment.GetEnvironmentVariable(contextEnvironmentVariable) ?? "production";
            });
        }

        public static void ConfigureConfigDefault(IHostBuilder builder, string[] args)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        // use https://marketplace.visualstudio.com/items?itemName=guitarrapc.OpenUserSecrets to easily manage UserSecrets with GenericHost.
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });
        }

        public static void ConfigureLoggingDefault(IHostBuilder builder)
        {
            builder.ConfigureLogging((hostingContext, logging) =>
            {
                logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
                if (hostingContext.HostingEnvironment.IsDevelopment())
                {
                    logging.AddDebug();
                }
            });
        }
    }
}

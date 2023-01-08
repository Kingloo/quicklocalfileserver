using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace quicklocalfileserver
{
	public static class Program
	{
		private const int port = 12_000;
		
		public static async Task<int> Main(string[] args)
		{
			CancellationTokenSource cts = new CancellationTokenSource();

			Console.CancelKeyPress += (object? _, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;

				cts.Cancel();
			};
			
			WebApplication webApplication = BuildWebApplication();

			try
			{
				await webApplication.RunAsync(cts.Token).ConfigureAwait(false);
			}
			finally
			{
				await webApplication.DisposeAsync().ConfigureAwait(false);

				cts.Dispose();
			}

			return 0;
		}

		private static WebApplication BuildWebApplication()
		{
			WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
			{
				ApplicationName = "quicklocalfileserver"
			});

			webAppBuilder.WebHost.ConfigureLogging((ILoggingBuilder loggingBuilder) =>
			{
				loggingBuilder.ClearProviders();

				loggingBuilder.SetMinimumLevel(LogLevel.Warning);

				loggingBuilder.AddSimpleConsole((SimpleConsoleFormatterOptions simpleConsoleFormatterOptions) =>
				{
					simpleConsoleFormatterOptions.ColorBehavior = LoggerColorBehavior.Enabled;
					simpleConsoleFormatterOptions.IncludeScopes = true;
					simpleConsoleFormatterOptions.SingleLine = true;
				});
			});

			webAppBuilder.WebHost.UseKestrel((KestrelServerOptions kestrelServerOptions) =>
			{
				kestrelServerOptions.ListenLocalhost(port);
			});
			
			WebApplication webApplication = webAppBuilder.Build();

			webApplication.UseStaticFiles(new StaticFileOptions
			{
				FileProvider = new PhysicalFileProvider(AppContext.BaseDirectory)
			});

			return webApplication;
		}
	}
}
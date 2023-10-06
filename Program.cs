using System;
using System.IO;
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
		private const int defaultPort = 12_000;
		private static readonly string defaultDirectory = AppContext.BaseDirectory;
		
		public static async Task<int> Main(string[] args)
		{
			ArgumentNullException.ThrowIfNull(args);

			CancellationTokenSource cts = new CancellationTokenSource();

			Console.CancelKeyPress += (object? _, ConsoleCancelEventArgs e) =>
			{
				e.Cancel = true;

				cts.Cancel();
			};

			int port = GetPort(args);
			string directory = GetDirectory(args);
			
			WebApplication webApplication = BuildWebApplication(port, directory);

			try
			{
				await webApplication.StartAsync(cts.Token).ConfigureAwait(false);

				await Console.Out.WriteLineAsync($"serving from '{directory}' on port {port}").ConfigureAwait(false);

				await webApplication.WaitForShutdownAsync(cts.Token).ConfigureAwait(false);
			}
			finally
			{
				await webApplication.DisposeAsync().ConfigureAwait(false);

				cts.Dispose();
			}

			return 0;
		}

		private static int GetPort(string[] args)
		{
			return GetValueFromCommandLineArgs(args, "-p") switch
			{
				string value => Int32.TryParse(value, out int result) switch
				{
					true => IsPortInRange(result)
						? result
						: throw new ArgumentOutOfRangeException($"invalid port number ('{result}')"),
					false => throw new ArgumentOutOfRangeException($"not an integer ('{value}')")
				},
				_ => defaultPort
			};
		}

		private static bool IsPortInRange(int port)
		{
			return port > 1023 && port < 65536;
		}

		private static string GetDirectory(string[] args)
		{
			string? absolutePath = defaultDirectory;

			if (GetValueFromCommandLineArgs(args, "-d") is string value)
			{
				if (value.Contains('~', StringComparison.Ordinal))
				{
					value = value.Replace(
						"~",
						Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
						StringComparison.Ordinal);
				}

				absolutePath = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
			}

			if (!Directory.Exists(absolutePath))
			{
				throw new DirectoryNotFoundException($"{absolutePath}");
			}

			return absolutePath;
		}

		private static string? GetValueFromCommandLineArgs(string[] args, string switchName)
		{
			for (int i = 0; i < args.Length; i++)
			{
				if (String.Equals(switchName, args[i], StringComparison.Ordinal))
				{
					if (i + 1 < args.Length)
					{
						return args[i + 1];
					}
				}
			}

			return null;
		}

		private static WebApplication BuildWebApplication(int port, string directory)
		{
			WebApplicationBuilder webAppBuilder = WebApplication.CreateBuilder(new WebApplicationOptions
			{
				ApplicationName = "quicklocalfileserver"
			});

			webAppBuilder.WebHost.ConfigureLogging(static (ILoggingBuilder loggingBuilder) =>
			{
				loggingBuilder.ClearProviders();

				loggingBuilder.SetMinimumLevel(LogLevel.Warning);

				loggingBuilder.AddSimpleConsole(static (SimpleConsoleFormatterOptions simpleConsoleFormatterOptions) =>
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
				FileProvider = new PhysicalFileProvider(directory)
			});

			return webApplication;
		}
	}
}
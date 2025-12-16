using Nager.PublicSuffix;
using Nager.PublicSuffix.RuleProviders;
using Newtonsoft.Json;
using PushoverClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using TeamFiltration.Helpers;
using TeamFiltration.Models.TeamFiltration;

namespace TeamFiltration.Handlers
{

	public class GlobalArgumentsHandler
	{
		public string OutPutPath { get; set; }

		public Config TeamFiltrationConfig { get; set; }
		public bool DebugMode { get; set; }
		public bool UsCloud { get; set; }
		public bool AADSSO { get; set; }
		public bool PushoverLocked { get; set; }
		public bool Pushover { get; set; }
		public int OwaLimit { get; set; }
		public bool AllowDirectConnection { get; set; }
		public string FlareProxEndpointsPath { get; set; }
		private Pushover _pushClient { get; set; }
		public AWSHandler _awsHandler { get; set; }
		public FlareProxHandler _flareProxHandler { get; set; }

		private DomainParser _domainParser { get; set; }
		public string[] AWSRegions { get; set; } = { "us-east-1", "us-west-1", "us-west-2", "ca-central-1", "eu-central-1", "eu-west-1", "eu-west-2", "eu-west-3", "eu-north-1" };
		public bool ADFS { get; internal set; }

		public GlobalArgumentsHandler(string[] args, DatabaseHandler databaseHandler, bool exfilModule = false)
		{

			//Really need to move away from this, but it's a quick fix for now
			var httpClient = new HttpClient();
			var cacheProvider = new Nager.PublicSuffix.RuleProviders.CacheProviders.LocalFileSystemCacheProvider();
			var ruleProvider = new CachedHttpRuleProvider(cacheProvider, httpClient);

			ruleProvider.BuildAsync().GetAwaiter().GetResult();

			_domainParser = new DomainParser(ruleProvider);

			OutPutPath = args.GetValue("--outpath");
			AADSSO = args.Contains("--aad-sso");

			PushoverLocked = args.Contains("--push-locked");
			Pushover = args.Contains("--push");
			UsCloud = args.Contains("--us-cloud");
			AllowDirectConnection = args.Contains("--allow-direct");
			this.DebugMode = args.Contains("--debug");


			var teamFiltrationConfigPath = args.GetValue("--config");
			string OwaLimitString = args.GetValue("--owa-limit");
			FlareProxEndpointsPath = args.GetValue("--flareprox-endpoints");



			if (string.IsNullOrEmpty(teamFiltrationConfigPath) && File.Exists("TeamFiltrationConfig.json"))
			{
				teamFiltrationConfigPath = "TeamFiltrationConfig.json";
			}

			if (!File.Exists(teamFiltrationConfigPath))
			{
				if (!exfilModule)
				{
					Console.WriteLine("[+] Could not find TeamFiltration config, provide a config path using with --config");
					return;
				}
				else
				{
					Console.WriteLine("[!] You are running TeamFiltration without a config");

				}

			}
			else
			{
				var configText = File.ReadAllText(teamFiltrationConfigPath);
				TeamFiltrationConfig = JsonConvert.DeserializeObject<Config>(configText);
			}


			Int32.TryParse(OwaLimitString, out var LocalOwaLimit);
			if (LocalOwaLimit > 0)
				OwaLimit = LocalOwaLimit;
			else
				OwaLimit = 2000;

			if (TeamFiltrationConfig == null)
			{
				TeamFiltrationConfig = new Config() { };
			}

			// Set default user agent if missing
			if (string.IsNullOrEmpty(TeamFiltrationConfig?.UserAgent))
				TeamFiltrationConfig.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Teams/1.3.00.30866 Chrome/80.0.3987.165 Electron/8.5.1 Safari/537.36";

			// Set default user agent if missing
			if (string.IsNullOrEmpty(TeamFiltrationConfig?.proxyEndpoint))
				TeamFiltrationConfig.proxyEndpoint = "http://127.0.0.1:8080";


			if (TeamFiltrationConfig?.AwsRegions?.Count() > 0)
			{
				AWSRegions = TeamFiltrationConfig.AwsRegions.ToArray();

			}

			try
			{
				if (!string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverUserKey) && !string.IsNullOrEmpty(TeamFiltrationConfig?.PushoverAppKey))
					_pushClient = new Pushover(TeamFiltrationConfig.PushoverAppKey);
			}
			catch (Exception ex)
			{

				Console.WriteLine($"[!] Failed to create Pushover client, bad API keys? -> {ex}");
			}


			//Do AWS FireProx generation checks
			if (!string.IsNullOrEmpty(TeamFiltrationConfig?.AWSSecretKey) && !string.IsNullOrEmpty(TeamFiltrationConfig?.AWSAccessKey))
			{
				_awsHandler = new AWSHandler(this.TeamFiltrationConfig.AWSAccessKey, this.TeamFiltrationConfig.AWSSecretKey, this.TeamFiltrationConfig.AWSSessionToken, databaseHandler);

			}

			//Initialize FlareProx handler
			try
			{
				// Use custom path if specified, otherwise default to "flareprox_endpoints.json"
				string filePath = !string.IsNullOrEmpty(FlareProxEndpointsPath) 
					? FlareProxEndpointsPath 
					: "flareprox_endpoints.json";
				
				// Try to find the file first for better error messages
				string exeDir = null;
				try
				{
					string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
					if (!string.IsNullOrEmpty(assemblyLocation))
					{
						exeDir = Path.GetDirectoryName(assemblyLocation);
					}
				}
				catch
				{
					// In single-file mode, try Process.GetCurrentProcess().MainModule.FileName
					try
					{
						exeDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
					}
					catch { }
				}
				
				string currentDir = Directory.GetCurrentDirectory();
				
				if (!string.IsNullOrEmpty(FlareProxEndpointsPath))
				{
					Console.WriteLine($"[*] Using custom FlareProx endpoints file: {filePath}");
				}
				else
				{
					Console.WriteLine($"[*] Looking for flareprox_endpoints.json...");
					if (!string.IsNullOrEmpty(exeDir))
						Console.WriteLine($"[*] Executable directory: {exeDir}");
					Console.WriteLine($"[*] Current directory: {currentDir}");
				}
				
				_flareProxHandler = new FlareProxHandler(filePath);
				
				if (_flareProxHandler != null && _flareProxHandler.EndpointCount > 0)
				{
					Console.WriteLine($"[+] Successfully loaded {_flareProxHandler.EndpointCount} FlareProx endpoints");
				}
				else
				{
					Console.WriteLine("[!] FlareProx handler initialized but no endpoints found");
					if (!AllowDirectConnection)
					{
						Console.WriteLine("[!] ERROR: FlareProx endpoints are required for the spray module");
						Console.WriteLine("[!] Make sure flareprox_endpoints.json exists and contains valid endpoints");
						Console.WriteLine("[!] Use --allow-direct flag to allow direct connections (NOT RECOMMENDED)");
						throw new Exception("FlareProx endpoints are required but not available");
					}
					_flareProxHandler = null;
				}
			}
			catch (FileNotFoundException ex)
			{
				if (!AllowDirectConnection)
				{
					Console.WriteLine($"[!] ERROR: {ex.Message}");
					Console.WriteLine("[!] FlareProx endpoints are REQUIRED for the spray module");
					if (string.IsNullOrEmpty(FlareProxEndpointsPath))
					{
						Console.WriteLine("[!] Make sure flareprox_endpoints.json is in the same directory as TeamFiltration.exe");
						Console.WriteLine("[!] Or specify the path using --flareprox-endpoints <path>");
					}
					else
					{
						Console.WriteLine($"[!] Make sure the file exists at: {FlareProxEndpointsPath}");
					}
					Console.WriteLine("[!] Use --allow-direct flag to allow direct connections (NOT RECOMMENDED - your IP will be logged)");
					throw;
				}
				else
				{
					Console.WriteLine($"[!] WARNING: {ex.Message}");
					Console.WriteLine("[!] Using direct connections (--allow-direct flag was set)");
					Console.WriteLine("[!] WARNING: Your IP address will be logged by Microsoft!");
					_flareProxHandler = null;
				}
			}
			catch (Exception ex)
			{
				if (!AllowDirectConnection)
				{
					Console.WriteLine($"[!] ERROR: Failed to load FlareProx endpoints: {ex.Message}");
					Console.WriteLine("[!] FlareProx endpoints are REQUIRED for the spray module");
					if (string.IsNullOrEmpty(FlareProxEndpointsPath))
					{
						Console.WriteLine("[!] Make sure flareprox_endpoints.json exists in the same directory as TeamFiltration.exe");
						Console.WriteLine("[!] Or specify the path using --flareprox-endpoints <path>");
					}
					else
					{
						Console.WriteLine($"[!] Make sure the file exists at: {FlareProxEndpointsPath}");
					}
					Console.WriteLine("[!] Use --allow-direct flag to allow direct connections (NOT RECOMMENDED - your IP will be logged)");
					throw new Exception($"FlareProx endpoints are required: {ex.Message}", ex);
				}
				else
				{
					Console.WriteLine($"[!] WARNING: Failed to load FlareProx endpoints: {ex.Message}");
					Console.WriteLine("[!] Using direct connections (--allow-direct flag was set)");
					Console.WriteLine("[!] WARNING: Your IP address will be logged by Microsoft!");
					_flareProxHandler = null;
				}
			}

		}
		public void PushAlert(string title, string message)
		{
			if (_pushClient != null)
			{
				if (Pushover || PushoverLocked)
				{
					try
					{
						PushResponse response = _pushClient.Push(
							title,
							message,
							TeamFiltrationConfig.PushoverUserKey
						);
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[!] Pushover message failed, error: {ex}");
					}
				}
			}

		}
		public string GetBaseUrl(string region = "US")
		{
			return "https://login.microsoftonline.com/common/oauth2/token";
		}

		public (Amazon.APIGateway.Model.CreateDeploymentRequest, Models.AWS.FireProxEndpoint, string fireProxUrl) GetFireProxURLObject(string url, int regionCounter)
		{
			// Return FlareProx endpoint URL instead of AWS FireProx
			// The url parameter is the target URL (e.g., https://login.microsoftonline.com)
			// We'll use FlareProx endpoints and set X-Target-URL header in the request
			try
			{
				if (_flareProxHandler != null && _flareProxHandler.EndpointCount > 0)
				{
					// Get next FlareProx endpoint using round-robin
					string flareProxUrl = _flareProxHandler.GetNextEndpoint();
					// Return the FlareProx endpoint URL (the target URL will be set via X-Target-URL header)
					return (null, null, flareProxUrl);
				}
			}
			catch (Exception ex)
			{
				// If FlareProx handler fails and direct connections are not allowed, throw error
				if (!AllowDirectConnection)
				{
					throw new Exception($"FlareProx endpoint unavailable: {ex.Message}. Use --allow-direct to allow direct connections (NOT RECOMMENDED)");
				}
				Console.WriteLine($"[!] WARNING: Error getting FlareProx endpoint: {ex.Message}");
				Console.WriteLine("[!] Falling back to direct connection (--allow-direct flag was set)");
			}
			
			// Only allow direct URL if explicitly permitted
			if (AllowDirectConnection)
			{
				Console.WriteLine("[!] WARNING: Using direct connection - your IP will be logged!");
				string fireProxUrl = url.TrimEnd('/');
				fireProxUrl += "/";
				return (null, null, fireProxUrl);
			}
			else
			{
				throw new Exception("FlareProx endpoints are required but not available. Use --allow-direct flag to allow direct connections (NOT RECOMMENDED)");
			}
		}
		private string EnsurePathChar(string outPutPath)
		{
			foreach (var invalidChar in Path.GetInvalidPathChars())
			{
				outPutPath = outPutPath.Replace(invalidChar, Path.DirectorySeparatorChar);
			}
			return outPutPath;
		}
	}
}
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace TeamFiltration.Handlers
{
    public class FlareProxHandler
    {
        private List<FlareProxEndpoint> _endpoints;
        private int _currentIndex;
        private readonly object _lockObject = new object();

        public class FlareProxEndpoint
        {
            public string name { get; set; }
            public string url { get; set; }
            public string created_at { get; set; }
        }

        public FlareProxHandler(string endpointsFilePath = "flareprox_endpoints.json")
        {
            LoadEndpoints(endpointsFilePath);
        }

        private void LoadEndpoints(string endpointsFilePath)
        {
            string fullPath = endpointsFilePath;
            string currentDir = Directory.GetCurrentDirectory();
            string exeDir = null;
            
            // If an absolute path is provided, use it directly
            if (Path.IsPathRooted(endpointsFilePath))
            {
                if (!File.Exists(endpointsFilePath))
                {
                    throw new FileNotFoundException($"FlareProx endpoints file not found at specified path: {endpointsFilePath}");
                }
                fullPath = endpointsFilePath;
            }
            else
            {
                // Try to find the file in multiple locations (relative path)
            
                // Priority 1: Try executable directory first (most reliable for published apps)
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
                    // In single-file mode, Location might throw or be empty
                    // Try using Process.GetCurrentProcess().MainModule.FileName instead
                    try
                    {
                        exeDir = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    }
                    catch
                    {
                        // If that also fails, exeDir will remain null
                    }
                }
                
                if (!string.IsNullOrEmpty(exeDir))
                {
                    string exePath = Path.Combine(exeDir, endpointsFilePath);
                    if (File.Exists(exePath))
                    {
                        fullPath = exePath;
                    }
                }
                
                // Priority 2: Try current working directory
                if (!File.Exists(fullPath))
                {
                    string currentPath = Path.Combine(currentDir, endpointsFilePath);
                    if (File.Exists(currentPath))
                    {
                        fullPath = currentPath;
                    }
                }
                
                // Priority 3: Try solution root (for development)
                if (!File.Exists(fullPath))
                {
                    string solutionRoot = Path.Combine(currentDir, "..", "..", "..", "..");
                    fullPath = Path.Combine(Path.GetFullPath(solutionRoot), endpointsFilePath);
                    
                    if (!File.Exists(fullPath))
                    {
                        // Try one more level up
                        solutionRoot = Path.Combine(currentDir, "..", "..", "..", "..", "..");
                        fullPath = Path.Combine(Path.GetFullPath(solutionRoot), endpointsFilePath);
                    }
                }
                
                // Priority 4: Try relative to executable location (parent directories)
                if (!File.Exists(fullPath) && !string.IsNullOrEmpty(exeDir))
                {
                    string parentPath = Path.Combine(exeDir, "..", endpointsFilePath);
                    fullPath = Path.GetFullPath(parentPath);
                }
            }

            if (!File.Exists(fullPath))
            {
                string searchedLocations = $"Current directory: {currentDir}";
                if (!string.IsNullOrEmpty(exeDir))
                {
                    searchedLocations += $"\nExecutable directory: {exeDir}";
                    searchedLocations += $"\nTried: {Path.Combine(exeDir, endpointsFilePath)}";
                }
                searchedLocations += $"\nTried: {Path.Combine(currentDir, endpointsFilePath)}";
                throw new FileNotFoundException($"FlareProx endpoints file not found: {endpointsFilePath}\nCreate endpoints using flareprox.py\nSearched locations:\n{searchedLocations}");
            }

            // Debug: Log where the file was found (only in debug mode)
            #if DEBUG
            Console.WriteLine($"[DEBUG] Found flareprox_endpoints.json at: {fullPath}");
            #endif

            string jsonContent = File.ReadAllText(fullPath);
            _endpoints = JsonConvert.DeserializeObject<List<FlareProxEndpoint>>(jsonContent);

            if (_endpoints == null || _endpoints.Count == 0)
            {
                throw new Exception($"No endpoints found in {endpointsFilePath}");
            }

            _currentIndex = 0;
        }

        /// <summary>
        /// Gets the next FlareProx endpoint URL using round-robin selection
        /// </summary>
        public string GetNextEndpoint()
        {
            lock (_lockObject)
            {
                if (_endpoints == null || _endpoints.Count == 0)
                {
                    throw new Exception("No FlareProx endpoints available");
                }

                string endpoint = _endpoints[_currentIndex].url;
                _currentIndex = (_currentIndex + 1) % _endpoints.Count;
                return endpoint;
            }
        }

        /// <summary>
        /// Gets a random FlareProx endpoint URL
        /// </summary>
        public string GetRandomEndpoint()
        {
            if (_endpoints == null || _endpoints.Count == 0)
            {
                throw new Exception("No FlareProx endpoints available");
            }

            Random rnd = new Random();
            return _endpoints[rnd.Next(_endpoints.Count)].url;
        }

        /// <summary>
        /// Gets all FlareProx endpoint URLs
        /// </summary>
        public List<string> GetAllEndpoints()
        {
            if (_endpoints == null || _endpoints.Count == 0)
            {
                throw new Exception("No FlareProx endpoints available");
            }

            return _endpoints.Select(e => e.url).ToList();
        }

        public int EndpointCount => _endpoints?.Count ?? 0;
    }
}

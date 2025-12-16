TeamFiltration_FlareProx.exe         ← Single executable (Most dependencies included)
├── SQLite.Interop.dll				 ← Required Dependency (Needs to be in the same directory as TeamFiltration_FlareProx.exe)
├── sni.dll 							 ← Required Dependency (Needs to be in the same directory as TeamFiltration_FlareProx.exe)
├── TeamFiltrationConfig.json       ← Your config file (copy manually into same directory as TeamFiltration_FlareProx.exe)
└── flareprox_endpoints.json        ← FlareProx endpoints (copy manually into same directory as TeamFiltration_FlareProx.exe)

Note: The usage is the same: 
e.g. .\TeamFiltration_FlareProx.exe  --outpath '[PATH/FOR/.db FILE]' --config .\TeamFiltrationConfig_Example.json --spray --usernames 'valid_users.txt' --passwords 'SeasonYear_Pass.txt' --domain example.com --shuffle-regions --jitter 60  --debug http://127.0.0.1:8080

The AWS keys can be ignored in the TeamFiltrationConfig.json as that feature has been disabled with this version.

The flareprox_endpoints.json is generated using flareprox.py --count [Amount of endpoints you want to spin up] 
If you have 2 endpoints in flareprox_endpoints.json and use --shuffle-regions, it will randomly pick between them for each spray attempt. (You don't really need to do this as a single endpoint always sends each request with a different IP address - 1 endpoint is enough)

https://github.com/MrTurvey/flareprox

(The normal teamfiltration tool uses AWS keys to auth to AWS gateway API and create new proxy APIs. teamfiltration_FlareProx does not do this. Cloudflare proxy APIs are created by flareprox.py and the teamfiltration_FlareProx.exe only wants the resulting flareprox_endpoints.json file)

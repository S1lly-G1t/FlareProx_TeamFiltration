<p align="center">
<img width="865" height="444" alt="Untitled Diagram drawio" src="https://github.com/user-attachments/assets/a1db99fd-131b-4ebb-8288-a132e681ddb5" />
</p>


### Note: The usage is the same

<pre lang=lisp>.\TeamFiltration_FlareProx.exe  --outpath '[PATH/FOR/.db FILE]' --config .\TeamFiltrationConfig_Example.json --spray --usernames 'valid_users.txt' --passwords 'SeasonYear_Pass.txt' --domain example.com --shuffle-regions --jitter 60  --debug http://127.0.0.1:8080</pre>

It will use your flareprox endpoints by default (by reading the ```flareprox_endpoints.json``` file) - unless you use ```--allow-direct``` which allows direct connections if FlareProx endpoints are unavailable (NOT RECOMMENDED - IP will be logged)

### The ```flareprox_endpoints.json``` is generated using [https://github.com/MrTurvey/flareprox](url)
<pre lang=lisp>
flareprox.py --count [Amount of endpoints you want to spin up]</pre>
If you have 2 endpoints in ```flareprox_endpoints.json``` and use ```--shuffle-regions```, it will randomly pick between them for each spray attempt. (You don't really need to do this as a single endpoint always sends each request with a different IP address - 1 endpoint is enough)

If you want to use the ```--enum --validate-teams``` module with flareprox, then you will need to copy the code in ```CloudFlareWorker.js``` and paste it inside your ```worker.js``` on [https://dash.cloudflare.com/](url)

(The normal teamfiltration tool uses AWS keys to auth to AWS gateway API and create new proxy APIs. ```teamfiltration_FlareProx.exe``` does not do this. Cloudflare proxy APIs are created by ```flareprox.py``` and the   ```teamfiltration_FlareProx.exe``` only wants the resulting ```flareprox_endpoints.json``` file in the same working directory, or specified using ```--flareprox-endpoints <Path>```)

The AWS keys can be ignored in the ```TeamFiltrationConfig.json``` as that feature has been disabled with this version.

### Compile code
In the `.sln` directory run:
<pre lang=lisp>dotnet restore</pre>
In the `.csproj` directory run:
<pre lang=lisp>dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:UseAppHost=true</pre>
The required files will be in the `\TeamFiltration\TeamFiltration\bin` directory and the OneDriveAPI .dll will be in `\TeamFiltration\OneDriveAPI\bin`

The following files need to be in the same directory when executing the tool:
<pre lang=lisp>
TeamFiltration_FlareProx.exe        ← Single executable (Most dependencies included)
├── SQLite.Interop.dll				      ← Required Dependency 
├── sni.dll 							          ← Required Dependency 
├── TeamFiltrationConfig.json       ← Your config file 
├── KoenZomers.OneDrive.Api         ← OneDriveAPI .dll 
└── flareprox_endpoints.json        ← FlareProx endpoints </pre>

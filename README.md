<p align="center">
<img width="865" height="444" alt="Untitled Diagram drawio" src="https://github.com/user-attachments/assets/a1db99fd-131b-4ebb-8288-a132e681ddb5" />
</p>

Amalgamation of [TeamFiltration](https://github.com/Flangvik/TeamFiltration) and [Flareprox](https://github.com/MrTurvey/flareprox) 🤝

# Install
Downloaded the latest .zip from the releases page.

The ```flareprox_endpoints.json``` is generated using [Flareprox](https://github.com/MrTurvey/flareprox)
<pre lang=lisp>
flareprox.py --count [Amount of endpoints you want to spin up]</pre>
> [!NOTE]  
> - If you have 2 endpoints in ```flareprox_endpoints.json``` and use ```--shuffle-regions```, it will randomly pick between them for each spray attempt. (You don't really need to do this as a single endpoint always sends each request with a different IP address)
> - The AWS keys can be ignored in the ```TeamFiltrationConfig.json``` as that feature has been disabled with this version.

> [!TIP]
> ### Compiling the source code (Optional):
> In the `.sln` directory run:
> <pre lang=lisp>dotnet restore</pre>
> In the `.csproj` directory run:
> <pre lang=lisp>dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:UseAppHost=true</pre>
> The required files will be in the `\TeamFiltration\TeamFiltration\bin` directory and the OneDriveAPI .dll will be in `\TeamFiltration\OneDriveAPI\bin`
>
> The following files need to be in the same directory when executing the tool:
> <pre lang=lisp>
> TeamFiltration_FlareProx.exe
> ├── SQLite.Interop.dll
> ├── sni.dll
> ├── TeamFiltrationConfig.json
> ├── KoenZomers.OneDrive.Api
> └── flareprox_endpoints.json
> </pre>  

# Usage:
### ```--spray``` module
<pre lang=lisp>.\TeamFiltration_FlareProx.exe  --outpath '[PATH/FOR/.db FILE]' --config .\TeamFiltrationConfig_Example.json --spray --usernames 'valid_users.txt' --passwords 'SeasonYear_Pass.txt' --domain example.com --shuffle-useragents --parallel 20 --jitter 60 </pre>
It will use your flareprox endpoints by default (by reading the ```flareprox_endpoints.json``` file in the same directory) - unless you use ```--allow-direct``` which allows direct connections without proxies.

> [!IMPORTANT]  
> If you have multiple useragents in your config file, E.G. ```"UserAgent": "UserAgent1%UserAgent2%UserAgent3"```, then you must specify ```--shuffle-useragents``` or the spray will fail.

> [!WARNING] 
> Use ```--parallel 20``` ```--jitter 60``` to make it spray users in batches of 20 with 60 second intervals between each batch (The default behaviour of the original teamfiltration tool) otherwise it will spray all users without delays.

### ```--enum``` module
The usage is the same as the original tool.
If you want to use the ```--enum --validate-teams``` module with flareprox proxies, then you will need to copy the code in ```CloudFlareWorker.js``` and paste it inside your ```worker.js``` on [https://dash.cloudflare.com/](url)

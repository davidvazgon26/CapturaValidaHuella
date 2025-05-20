# Lectura de huellas ZK Teco 

Nota.- Los acentos se omiten arbitrareamente.

Permite la lectura de huellas y validacion por medio de servicio de endpoint.

Se instala en local y las peticiones son a:

http://localhost:8080/capturar (GET)
http://localhost:8080/validar  (POST, enviar solo template_base64)

* Valida que el dispositivo este conectado
* Activacion de led para indicar que coloques huella en lector
* Requiere dll libzkfpcsharp.dll

Se realizaron unas modificaciones para que:

* Se ejecute como servicio de windows (se comenta codigo anterior para que lo puedan usar)
* Compilar en modo Release a x64 (el dll libskfpcsharp.dll integrado es solo para x64, si deseas consigue el dll y cambialo a x86)

Para la instalación y puesta en marcha como servicio de windows te pueden servir los siguientes comandos:

***Nota, ejecutar terminal o CMD como Admin*

* Ir a la ruta del instalador de servicios de windows

  ````
  cd C:\Windows\Microsoft.NET\Framework64\v4.0.30319
  ````
* Instalacion con parametros del ejecutable:

  ````
  InstallUtil.exe /LogToConsole=false /SkipEventLogSource=true "TuRuta\Completa\Al\Servicio\bin\Release\ZK9500.Fingerprint.Service.exe"
  ````

  * Para Desinstalar
    ````
    sc delete ZK9500FingerprintService
    ````

  * Comprobar si se instalo correctamente
    ````
    sc query ZK9500FingerprintService
    ````

  * Iniciar / Detener servicio
    ````
    net start ZK9500FingerprintService
    net stop ZK9500FingerprintService
    ````

  * Si quieres validar instalación / iniciar / Detener el servicio desde UI usa:
    ````
    win + r
    en el input (abrit) escribe: services.msc
    Buscas en los servicios por Nombre "Servicio de Huellas ZK9500"
    Ahi puedes ver estado / iniciar / detener el servicio
    ````


    ## Pasos para generar instalador como servicio de Windows

    * Despues de compilar el proyecto y tner nuestro ejecutable (en la ruta bin/Release) procedemos a crear el instalador (msi) con Wix Tools
    * En una carpeta nueva en cualquier directorio de nuestro equipo creamos la siguiente estructura y copiamos y pegamos los ejecutables, dependencias y drivers:
   
      ````
      📦Instalador (yo le puse asi, tu escoge el nombre que quieras.)
      ├──📂Drivers
      │   └── zkdriver.sys      # Drivers del ZK9500 (lo pongo como referencia, pero en este caso en particular no tenemos drivers, se instalan por separado de ser necesarios)
      ├──📂DLLs
      │   └── libzkfpcsharp.dll # Dependencia
      └──📂App
          ├── ZK9500.Fingerprint.Service.exe
          ├── ZK9500.Fingerprint.Service.exe.config
          └── setup.exe         # Drivers (opcional, yo no lo inclui)
      ````

      * Instalar Wix tool set. [link](https://www.firegiant.com/wixtoolset/) seguir el link y las instrucciones.
      * Tambien puedes hacerlo desde la linea de comandos de PowerShell con choco (si no lo tienes, tambien hay que instalar)
          * Pasos para instalar choco desde PowerShell.
            *   Abrir Powershell como Admin y correr los siguientes comandos:
           
              instalar choco (chocolatey)
              ````
              Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex     
              ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
              ````

             Validar correcta instalación.
             ````
             choco --version
             ````
             
             Instalar Wix tool set
             ````
             choco install wixtoolset -y
             ````
             
             Validar correcta instalación.
             ````
             Get-ChildItem -Path "C:\Program Files*\WiX Toolset*" -Recurse -Include "candle.exe", "light.exe" -ErrorAction SilentlyContinue
             ````
             
             Validar correcta de herramientas de Wix.
             ````
             where.exe candle
             where.exe light
             ````
     
      * Despues de instalar Wix Tools set y crear nuestra carpetas, vamos a la raiz donde debemos tener el archivo Product.wxs con un codigo como este (ojo, debes manipular algunas     
        secciones en tu propia versión)
        
             
             ````
             <?xml version="1.0" encoding="UTF-8"?>
            <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
              <Product Id="*" Name="Servicio de Huellas ZK9500" Language="1033" Version="1.0.0" Manufacturer="TuEmpresa" UpgradeCode="Tu Propio GUID va aqui" Codepage="65001">
                
                <Package InstallerVersion="200" Compressed="yes" InstallScope="perMachine" />
                <Media Id="1" Cabinet="media1.cab" EmbedCab="yes" />
                
                <MajorUpgrade DowngradeErrorMessage="Ya existe una versión más reciente instalada." />
                
                <Feature Id="MainFeature" Title="Servicio Principal" Level="1">
                  <ComponentGroupRef Id="ServiceComponents" />
                  <ComponentGroupRef Id="DriverComponents" />
                </Feature>
                
                <Directory Id="TARGETDIR" Name="SourceDir">
                  <Directory Id="ProgramFilesFolder">
                    <Directory Id="INSTALLFOLDER" Name="ZKService" />
                  </Directory>
                  <Directory Id="SystemFolder" Name="System32">
                    <Directory Id="DriversFolder" Name="drivers" />
                  </Directory>
                </Directory>
                
                <!-- Servicio Windows -->
                <ComponentGroup Id="ServiceComponents" Directory="INSTALLFOLDER">
                  <Component Id="MainExecutable" Guid="*">
                    <File Id="ZKService.exe" Source="App\ZK9500.Fingerprint.Service.exe" KeyPath="yes" />
                    <ServiceInstall 
                      Id="ServiceInstaller" 
                      Type="ownProcess" 
                      Name="ZKService" 
                      DisplayName="Servicio de Huellas ZK9500"  <!-- Cambialo por lo que tu quieras -->
                      Description="Controla el lector ZK9500"   <!-- Cambialo si quieres -->
                      Start="auto" 
                      Account="LocalSystem" 
                      ErrorControl="normal" />
                    <ServiceControl 
                      Id="StartService" 
                      Name="ZKService" 
                      Start="install" 
                      Stop="both" 
                      Remove="uninstall" 
                      Wait="yes" />
                  </Component>
                  
                  <Component Id="AppConfig" Guid="*">
                    <File Id="AppConfig" Source="App\ZK9500.Fingerprint.Service.exe.config" KeyPath="yes" />
                  </Component>
                  
                  <Component Id="LibDLL" Guid="*">
                    <File Id="LibDLL" Source="DLLs\libzkfpcsharp.dll" KeyPath="yes" />
                  </Component>
                </ComponentGroup>
                
                <!-- Drivers (vacío pero mantiene la estructura) -->
                <ComponentGroup Id="DriverComponents" Directory="DriversFolder">
                </ComponentGroup>
                
              </Product>
            </Wix>
             ````
        *  Desde PowerShell puedes crear un GUID para insertar en este codigo con la siguiente instruccion:
       
          ````
           [guid]::NewGuid().ToString().ToUpper()
          ````

        * Guarda los cambios y ya estamos listos para generar el instalador con los siguientes comandos (si no pudiste llegar hasta aqui probablemente surgierron algunos errores de 
          instalacion que debes recolver por tu cuenta):

          ````
          candle.exe "Product.wxs" -ext WixUtilExtension

          light.exe "Product.wixobj" -out "ZKServiceInstaller.msi" -ext WixUtilExtension

          ````
     
        * Esto te debio generar tu instalador.msi listo para usarse, ya puedes utilizarlo en tu primera instalacion.

        * Puedes validar la correcta instalacion con este comando.
          
         ````
         Get-Service "ZKService" | Select-Object Name, Status, StartType
         ````

        * el ejecutable y su dependencia debe estar enb la ruta:  "C:\Program Files\ZKService\" (o "Archivos de Programas" en lugar de Program File para versión en español)
       
        * Otros comandos:
       
          * Instalar desde la linea de comandos:
            ````
            msiexec /i "ZKServiceInstaller.msi" /quiet /norestart
            ````

            * Instalación con logs de la instalacion:
              ````
              msiexec /i "ZKServiceInstaller.msi" /l*v "install_log.txt"
              ````

            * Instalar/Desinstalar:
              ````
                msiexec /i "ZKServiceInstaller.msi" /quiet

                msiexec /x "ZKServiceInstaller.msi" /quiet
              ````

      

      
   
    

    

  

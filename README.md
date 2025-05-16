# Lectura de huellas ZK Teco 

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

    

  

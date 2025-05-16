# Lectura de huellas ZK Teco 

Permite la lectura de huellas y validacion por medio de servicio de endpoint.

Se instala en local y las peticiones son a:

http://localhost:8080/capturar (GET)
http://localhost:8080/validar  (POST, enviar solo template_base64)

* Valida que el dispositivo este conectado
* Activacion de led para indicar que coloques huella en lector
* Requiere dll libzkfpcsharp.dll

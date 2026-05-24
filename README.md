# AppBusinessIntelligence
Aplicación móvil Android para **análisis de datasets**, **generación de informes en PDF**, **autenticación segura con Firebase** y **doble verificación TOTP**.  
Incluye un **modo demo** con 3 datasets de ejemplo para probar la app sin necesidad de registro.

---

## Índice

- [Descripción general](#descripción-general)
- [Características principales](#características-principales)
- [Tecnologías utilizadas](#tecnologías-utilizadas)
- [Instalación](#instalación)
- [Modo demo](#modo-demo)
- [Arquitectura](#arquitectura)
- [Diagrama de flujo](#diagrama-de-flujo)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Capturas de pantalla](#capturas-de-pantalla)
  

---

## Descripción general

**Analystix** es una aplicación móvil desarrollada en **.NET MAUI** orientada al análisis de datasets y la generación de informes en formato PDF.  
Permite a los usuarios:

- Analizar datos de forma rápida.
- Generar informes profesionales en PDF.
- Guardar los informes en carpetas locales mediante **SAF (Storage Access Framework)**.
- Acceder a un **modo demo** sin registro.
- Autenticarse con **Firebase** y **doble factor (2FA) mediante TOTP**.

---

## Características principales

- 🔐 **Autenticación con Firebase** (login, registro, recuperación de contraseña).
- 🔑 **Doble verificación TOTP (2FA)**.
- 📊 **Análisis de datasets**.
- 📄 **Generación de informes en PDF**.
- 📁 **Guardado de informes mediante SAF (Android)**.
- 🧪 **Modo demo con 3 datasets preanalizados e informes ya generados**.
- 🎨 **Interfaz moderna en .NET MAUI**.
- 🌙 **Soporte de tema claro/oscuro**.
- ⏳ **Gestión de inactividad y sesión expirada**.

---

## Tecnologías utilizadas

- **.NET MAUI** (aplicación móvil multiplataforma, objetivo Android).
- **Firebase Authentication** (gestión de usuarios).
- **Firebase Realtime Database / Firestore** (según implementación).
- **TOTP (Time-based One-Time Password)** para 2FA.
- **SAF (Storage Access Framework)** para selección de carpetas y guardado de PDFs.
- Servicios propios:
  - `AuthService`
  - `FirebaseAuthService`
  - `DatabaseService`
  - `DatasetAnalyzerService`
  - `GeneradorPdfService`
  - `TotpService`
  - `FileSaverService`
  - `FolderPickerService`
  - `InactivityService`
  - `ThemeService`

---

## Instalación

### Descargar APK

Puedes descargar la última versión de Analystix desde:

**APK:**  
<img width="300" height="300" alt="qr-code tfg" src="https://github.com/user-attachments/assets/3f45992e-0143-43be-a827-2e8d6676b14a" />

### Instalación en Android

1. Copia el APK al dispositivo o escanea el código QR .
2. Activa la instalación desde orígenes desconocidos.
3. Abre el archivo APK y sigue los pasos del instalador.
## Modo demo

Analystix incluye un **modo demo** que permite:

- Probar la aplicación sin necesidad de crear una cuenta.
- Acceder a **3 datasets de ejemplo**.
- Ver el **análisis ya generado** para esos datasets.
- Visualizar los **informes PDF de ejemplo**.
- Al finalizar, el usuario puede decidir **registrarse** para usar la app completa con sus propios datos.

---

## Arquitectura

La arquitectura de Analystix sigue una separación en **Modelo – Vista – Servicios**.
<img width="2174" height="473" alt="Diagrama de arquitectura" src="https://github.com/user-attachments/assets/607e9bd0-de03-4a57-99aa-cf418dbf32a4" />

---

## Diagrama de flujo
<img width="933" height="1018" alt="Diagrama de flujo" src="https://github.com/user-attachments/assets/43606ea2-358f-494a-9ad0-5f9156e5f37b" />

---

## Diagrama de uso 
<img width="519" height="788" alt="Diagrama de uso" src="https://github.com/user-attachments/assets/e3a897f3-b674-4105-8178-f5458d51d680" />


---

## Estructura del proyecto
<img width="676" height="1005" alt="image" src="https://github.com/user-attachments/assets/8dc10d7e-e200-4c46-96e0-c702bb641920" />



---

## Capturas de pantalla
<img width="280" alt="image" src="https://github.com/user-attachments/assets/4d6b8eb5-7fbe-408c-a852-2b18626472b3" />
<img width="280" alt="image" src="https://github.com/user-attachments/assets/51b03955-245e-44c1-a644-b43867d197d9" />
<img width="280" alt="image" src="https://github.com/user-attachments/assets/709bf6ef-a32e-4f3d-964f-9c7f569b9ee3" />
<img width="280" alt="image" src="https://github.com/user-attachments/assets/141d1fdd-1c90-4b66-a5c4-4136a2cba6b0" />
<img width="280" alt="image" src="https://github.com/user-attachments/assets/504d3970-3b04-433e-b4b3-5067b44d526f" />







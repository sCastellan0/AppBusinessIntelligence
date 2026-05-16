# Sistema de Autenticación - TFG APP Business Intelligence

## 📋 Estructura Implementada

### Carpetas creadas:
- **Models**: Entidades de la base de datos
- **Services**: Lógica de negocio

### Archivos creados:

1. **Models/Usuario.cs**
   - Modelo de usuario con atributos SQLite
   - Campos: Id, NombreUsuario, PasswordHash, Correo, Telefono, FechaRegistro, UltimoAcceso

2. **Services/DatabaseService.cs**
   - Manejo de la base de datos SQLite
   - CRUD de usuarios
   - Ubicación de BD: `FileSystem.AppDataDirectory/tfgapp.db3`

3. **Services/AuthService.cs**
   - Registro de usuarios
   - Inicio de sesión
   - Gestión de sesión actual
   - Hash de contraseñas con SHA256

## 🔐 Funcionalidades Implementadas

### Registro de Usuario:
- ✅ Validación de campos requeridos
- ✅ Validación de contraseña (mínimo 4 caracteres)
- ✅ Verificación de usuario único
- ✅ Hash seguro de contraseñas
- ✅ Almacenamiento de correo y teléfono

### Inicio de Sesión:
- ✅ Validación de credenciales
- ✅ Verificación de contraseña hasheada
- ✅ Actualización de último acceso
- ✅ Gestión de sesión activa

### Dashboard:
- ✅ Muestra usuario autenticado
- ✅ Opción de cerrar sesión
- ✅ Navegación segura

## 🚀 Cómo Funciona

### 1. Registro de Usuario:
```
Usuario -> Página Registro -> Ingresa datos -> AuthService.RegistrarUsuarioAsync()
-> Valida datos -> Hash de contraseña -> DatabaseService.SaveUsuarioAsync()
-> SQLite guarda usuario -> Retorna a Login
```

### 2. Inicio de Sesión:
```
Usuario -> Página Login -> Ingresa credenciales -> AuthService.IniciarSesionAsync()
-> Busca usuario en BD -> Verifica contraseña hasheada -> Actualiza último acceso
-> Establece usuario actual -> Navega a Dashboard
```

### 3. Cierre de Sesión:
```
Usuario -> Dashboard -> Clic en Salir -> AuthService.CerrarSesion()
-> Limpia usuario actual -> Navega a Login
```

## 📦 Paquetes NuGet Instalados

- **sqlite-net-pcl** (v1.9.172): ORM para SQLite
- **SQLitePCLRaw.bundle_green**: Proveedor de SQLite nativo

## 🔧 Inyección de Dependencias

Configurado en `MauiProgram.cs`:
- `DatabaseService`: Singleton (única instancia)
- `AuthService`: Singleton (mantiene sesión)
- Páginas: Transient (nueva instancia cada vez)

## 💾 Base de Datos

**Ubicación**: 
- Android: `/data/data/com.companyname.tfg_appbusinessintelligence/files/tfgapp.db3`
- iOS: `~/Library/tfgapp.db3`
- Windows: `%LOCALAPPDATA%/tfgapp.db3`

**Tabla usuarios**:
```sql
CREATE TABLE usuarios (
	Id INTEGER PRIMARY KEY AUTOINCREMENT,
	NombreUsuario TEXT UNIQUE NOT NULL,
	PasswordHash TEXT NOT NULL,
	Correo TEXT,
	Telefono TEXT,
	FechaRegistro DATETIME,
	UltimoAcceso DATETIME
);
```

## ✅ Próximos Pasos Sugeridos

1. **Mejorar seguridad**:
   - Agregar salt a las contraseñas
   - Implementar tokens de sesión
   - Agregar expiración de sesión

2. **Validaciones adicionales**:
   - Formato de correo electrónico
   - Formato de teléfono
   - Fortaleza de contraseña

3. **Funcionalidades extra**:
   - Recuperación de contraseña
   - Editar perfil de usuario
   - Cambio de contraseña

4. **Backend (opcional)**:
   - API REST para sincronización
   - Almacenamiento en la nube
   - Autenticación con OAuth

## 🧪 Cómo Probar

1. Ejecuta la aplicación
2. Ve a "Registro" desde la página de Login
3. Crea un usuario con:
   - Nombre de usuario
   - Teléfono
   - Correo
   - Contraseña (mínimo 4 caracteres)
4. Vuelve a Login e inicia sesión con las credenciales
5. Verás el Dashboard con tu nombre de usuario
6. Prueba cerrar sesión

## 📱 Persistencia

Los datos se mantienen incluso si cierras la app, ya que están en SQLite local.

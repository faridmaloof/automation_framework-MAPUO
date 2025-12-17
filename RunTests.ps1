# MAPUO Test Execution Scripts
# Facilita la ejecucion de pruebas con diferentes configuraciones

# Script 1: Ejecutar todas las pruebas E2E con navegador visible
function Run-E2E-Visible {
    Write-Host "Ejecutando pruebas E2E con navegador visible..." -ForegroundColor Green
    $env:HEADLESS = "false"
    $env:BROWSER = "chromium"
    dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj --logger "console;verbosity=detailed"
}

# Script 2: Ejecutar pruebas E2E en modo headless (CI/CD)
function Run-E2E-Headless {
    Write-Host "Ejecutando pruebas E2E en modo headless..." -ForegroundColor Green
    $env:HEADLESS = "true"
    $env:BROWSER = "chromium"
    dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
}

# Script 3: Ejecutar solo pruebas smoke
function Run-Smoke-Tests {
    Write-Host "Ejecutando pruebas smoke..." -ForegroundColor Yellow
    $env:HEADLESS = "false"
    dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj --filter "Category=smoke"
}

# Script 4: Ejecutar pruebas en todos los navegadores
function Run-All-Browsers {
    # Intentar cargar configuración desde webconfig.json
    $configPath = Join-Path $PSScriptRoot "tests\E2E\MAPUO.Tests.E2E\webconfig.json"
    $browsers = @("chromium", "firefox", "webkit") # Valores por defecto

    if (Test-Path $configPath) {
        try {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.Browsers -and $config.Browsers.Count -gt 0) {
                $browsers = $config.Browsers
                Write-Host "Usando navegadores configurados: $($browsers -join ', ')" -ForegroundColor Green
            } else {
                Write-Host "Usando navegadores por defecto: $($browsers -join ', ')" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "Error al cargar configuración, usando valores por defecto: $($browsers -join ', ')" -ForegroundColor Red
        }
    } else {
        Write-Host "Archivo de configuración no encontrado, usando valores por defecto: $($browsers -join ', ')" -ForegroundColor Yellow
    }

    foreach ($browser in $browsers) {
        Write-Host ""
        Write-Host "Ejecutando pruebas en $browser..." -ForegroundColor Cyan
        $env:BROWSER = $browser
        $env:HEADLESS = "true"
        dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
    }
}

# Script 5: Limpiar y reconstruir solucion
function Clean-Build {
    Write-Host "Limpiando solucion..." -ForegroundColor Magenta
    dotnet clean
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue bin,obj,TestResults
    
    Write-Host "Compilando solucion..." -ForegroundColor Magenta
    dotnet build
}

# Script 6: Setup completo del proyecto
function Setup-Project {
    Write-Host "Configurando proyecto MAPUO..." -ForegroundColor Blue
    
    Write-Host "1. Restaurando dependencias..." -ForegroundColor Blue
    dotnet restore
    
    Write-Host "2. Compilando solucion..." -ForegroundColor Blue
    dotnet build
    
    Write-Host "3. Instalando navegadores Playwright..." -ForegroundColor Blue
    & ".\tests\E2E\MAPUO.Tests.E2E\bin\Debug\net9.0\playwright.ps1" install
    
    Write-Host "Setup completo!" -ForegroundColor Green
}

# Script 7: Generar y abrir reporte Allure
function Open-Allure-Report {
    Write-Host "Generando reporte Allure..." -ForegroundColor Cyan
    
    if (!(Get-Command allure -ErrorAction SilentlyContinue)) {
        Write-Host "Allure no esta instalado. Instalando..." -ForegroundColor Red
        npm install -g allure-commandline
    }
    
    allure serve allure-results
}

# Script 8: Ejecutar pruebas con reporte Allure
function Run-With-Allure {
    Write-Host "Ejecutando pruebas con reporte Allure..." -ForegroundColor Green
    
    # Limpiar resultados anteriores
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue allure-results
    
    # Ejecutar pruebas
    $env:HEADLESS = "true"
    dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
    
    # Generar y abrir reporte
    Open-Allure-Report
}

# Script 8.1: Ejecutar pruebas en múltiples navegadores con Allure
function Run-All-Browsers-With-Allure {
    Write-Host "Ejecutando pruebas en múltiples navegadores con reporte Allure..." -ForegroundColor Green
    
    # Limpiar resultados anteriores
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue allure-results
    
    # Intentar cargar configuración desde webconfig.json
    $configPath = Join-Path $PSScriptRoot "tests\E2E\MAPUO.Tests.E2E\webconfig.json"
    $browsers = @("chromium", "firefox", "webkit") # Valores por defecto

    if (Test-Path $configPath) {
        try {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.Browsers -and $config.Browsers.Count -gt 0) {
                $browsers = $config.Browsers
                Write-Host "Usando navegadores configurados: $($browsers -join ', ')" -ForegroundColor Green
            } else {
                Write-Host "Usando navegadores por defecto: $($browsers -join ', ')" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "Error al cargar configuración, usando valores por defecto: $($browsers -join ', ')" -ForegroundColor Red
        }
    } else {
        Write-Host "Archivo de configuración no encontrado, usando valores por defecto: $($browsers -join ', ')" -ForegroundColor Yellow
    }

    foreach ($browser in $browsers) {
        Write-Host ""
        Write-Host "Ejecutando pruebas en $browser con Allure..." -ForegroundColor Cyan
        $env:BROWSER = $browser
        $env:HEADLESS = "true"
        # Ejecutar pruebas y permitir que cada ejecución genere su propio allure-results
        dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj

        # Después de la ejecución, mover los resultados generados (si existen) a una carpeta por navegador
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $srcResults = Join-Path $PSScriptRoot "allure-results"
        if (Test-Path $srcResults) {
            $destDir = Join-Path $PSScriptRoot (Join-Path "allure-results-by-browser" "$browser-$timestamp")
            Write-Host "Moviendo resultados de Allure a: $destDir" -ForegroundColor DarkCyan
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
            Get-ChildItem -Path $srcResults -File | ForEach-Object {
                Copy-Item -Path $_.FullName -Destination $destDir -Force
            }
            # Limpiar src to avoid mixing with next run
            Get-ChildItem -Path $srcResults -File | ForEach-Object { Remove-Item -Path $_.FullName -Force }
        } else {
            Write-Host "No se encontraron resultados de Allure para $browser" -ForegroundColor Yellow
        }
    }
    
    # Consolidar todas las carpetas por navegador en una sola carpeta 'allure-results'
    $finalResults = Join-Path $PSScriptRoot "allure-results"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $finalResults
    New-Item -ItemType Directory -Force -Path $finalResults | Out-Null

    $splitRoot = Join-Path $PSScriptRoot "allure-results-by-browser"
    if (Test-Path $splitRoot) {
        Get-ChildItem -Path $splitRoot -Directory | ForEach-Object {
            Copy-Item -Path (Join-Path $_.FullName "*") -Destination $finalResults -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Generar y abrir reporte consolidado
    Open-Allure-Report
}

# Script 9: Ejecutar solo una categoria especifica
function Run-By-Category {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Category
    )
    
    Write-Host "Ejecutando pruebas de categoria: $Category" -ForegroundColor Yellow
    $env:HEADLESS = "false"
    dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj --filter "Category=$Category"
}

# Script 10: Ver estructura del proyecto
function Show-Project-Structure {
    Write-Host ""
    Write-Host "Estructura del proyecto MAPUO:" -ForegroundColor Cyan
    Write-Host ""
    tree /F /A
}

# Mensaje de ayuda
function Show-Help {
    Write-Host ""
    Write-Host "MAPUO Test Execution Scripts" -ForegroundColor Green
    Write-Host "============================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Comandos disponibles:" -ForegroundColor Yellow
    Write-Host "  Setup-Project          - Configura el proyecto completo" -ForegroundColor White
    Write-Host "  Run-E2E-Visible        - Ejecuta pruebas E2E con navegador visible" -ForegroundColor White
    Write-Host "  Run-E2E-Headless       - Ejecuta pruebas E2E en modo headless" -ForegroundColor White
    Write-Host "  Run-Smoke-Tests        - Ejecuta solo pruebas smoke" -ForegroundColor White
    Write-Host "  Run-All-Browsers       - Ejecuta pruebas en todos los navegadores" -ForegroundColor White
    Write-Host "  Run-All-Browsers-With-Allure - Ejecuta pruebas en todos los navegadores con reporte Allure" -ForegroundColor White
    Write-Host "  Clean-Build            - Limpia y recompila la solucion" -ForegroundColor White
    Write-Host "  Open-Allure-Report     - Genera y abre reporte Allure" -ForegroundColor White
    Write-Host "  Run-With-Allure        - Ejecuta pruebas y genera reporte Allure" -ForegroundColor White
    Write-Host "  Run-By-Category [cat]  - Ejecuta pruebas por categoria" -ForegroundColor White
    Write-Host "  Show-Project-Structure - Muestra la estructura del proyecto" -ForegroundColor White
    Write-Host "  Show-Help              - Muestra esta ayuda" -ForegroundColor White
    Write-Host ""
    
    Write-Host "Ejemplos:" -ForegroundColor Yellow
    Write-Host "  Run-E2E-Visible" -ForegroundColor Gray
    Write-Host "  Run-By-Category -Category smoke" -ForegroundColor Gray
    Write-Host "  Run-With-Allure" -ForegroundColor Gray
    Write-Host ""
}

# Mostrar ayuda al cargar el script
Write-Host ""
Write-Host "Script de ejecucion MAPUO cargado exitosamente!" -ForegroundColor Green
Write-Host "Ejecuta Show-Help para ver los comandos disponibles." -ForegroundColor Cyan
Write-Host ""

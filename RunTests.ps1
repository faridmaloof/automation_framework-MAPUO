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

# Script 8.1: Ejecutar pruebas en múltiples navegadores con Allure (VERSIÓN PROFESIONAL)
function Run-All-Browsers-With-Allure {
    Write-Host "`n=== MAPUO - Ejecución Multi-Browser con Allure ===" -ForegroundColor Green
    Write-Host "Framework: Clean Architecture + Screenplay Pattern" -ForegroundColor DarkGray
    Write-Host "Lider Tecnico: 30+ años experiencia Dev + QA`n" -ForegroundColor DarkGray
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $repoRoot = $PSScriptRoot
    
    # Limpiar resultados anteriores
    Write-Host "[1/5] Limpiando resultados anteriores..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$repoRoot\allure-results"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$repoRoot\allure-results-by-browser"
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue "$repoRoot\TestResults"
    
    # Cargar configuración
    Write-Host "[2/5] Cargando configuración de navegadores..." -ForegroundColor Cyan
    $configPath = Join-Path $repoRoot "tests\E2E\MAPUO.Tests.E2E\webconfig.json"
    $browsers = @("chromium", "firefox", "webkit")

    if (Test-Path $configPath) {
        try {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.Browsers -and $config.Browsers.Count -gt 0) {
                $browsers = $config.Browsers
                Write-Host "  ✓ Navegadores configurados: $($browsers -join ', ')" -ForegroundColor Green
            }
        } catch {
            Write-Host "  ⚠ Error cargando config, usando default: $($browsers -join ', ')" -ForegroundColor Yellow
        }
    }

    # Preparar directorios
    $allureFinalDir = Join-Path $repoRoot "allure-results"
    $allureBrowserDir = Join-Path $repoRoot "allure-results-by-browser"
    New-Item -ItemType Directory -Force -Path $allureFinalDir | Out-Null
    New-Item -ItemType Directory -Force -Path $allureBrowserDir | Out-Null

    # Ejecutar pruebas por navegador
    Write-Host "[3/5] Ejecutando pruebas en múltiples navegadores...`n" -ForegroundColor Cyan
    
    $totalTests = 0
    $totalPassed = 0
    $totalFailed = 0
    $browserResults = @{}

    foreach ($browser in $browsers) {
        Write-Host "  ┌─ Navegador: $($browser.ToUpper())" -ForegroundColor Magenta
        Write-Host "  │  Configurando ambiente..." -ForegroundColor DarkGray
        
        $env:BROWSER = $browser
        $env:CURRENT_BROWSER = $browser
        $env:HEADLESS = "true"
        $env:TEST_ENV = "CI"
        
        # Preparar directorio específico para este navegador
        $browserResultDir = Join-Path $allureBrowserDir "$browser-$timestamp"
        New-Item -ItemType Directory -Force -Path $browserResultDir | Out-Null
        
        # Configurar Allure para escribir en directorio específico
        $env:ALLURE_RESULTS_DIRECTORY = $browserResultDir
        $env:ALLURE_CONFIG = Join-Path $repoRoot "tests\E2E\MAPUO.Tests.E2E\allureConfig.json"
        
        Write-Host "  │  Ejecutando tests..." -ForegroundColor DarkGray
        $testOutput = dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj `
            --logger "console;verbosity=minimal" `
            --logger "trx;LogFileName=$browser-results.trx" `
            --results-directory "$repoRoot\TestResults\$browser" `
            2>&1
        
        $exitCode = $LASTEXITCODE
        
        # Parsear resultados
        $testOutput | ForEach-Object {
            if ($_ -match "Total de pruebas:\s*(\d+)") { $tests = [int]$Matches[1] }
            if ($_ -match "Correctas:\s*(\d+)") { $passed = [int]$Matches[1] }
            if ($_ -match "Con errores:\s*(\d+)") { $failed = [int]$Matches[1] }
        }
        
        if ($tests) {
            $totalTests += $tests
            $totalPassed += $passed
            $totalFailed += $failed
            $browserResults[$browser] = @{
                Total = $tests
                Passed = $passed
                Failed = $failed
            }
            Write-Host "  │  Resultados: $passed/$tests ✓" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Yellow" })
        } else {
            Write-Host "  │  ⚠ No se pudieron parsear resultados" -ForegroundColor Yellow
        }
        
        # Copiar resultados Allure generados
        if (Test-Path $browserResultDir) {
            $jsonFiles = Get-ChildItem -Path $browserResultDir -Filter "*.json" -File
            if ($jsonFiles.Count -gt 0) {
                Write-Host "  │  ✓ Allure: $($jsonFiles.Count) archivos JSON generados" -ForegroundColor Green
                Copy-Item -Path "$browserResultDir\*" -Destination $allureFinalDir -Recurse -Force
            } else {
                Write-Host "  │  ⚠ Allure: No se generaron archivos JSON" -ForegroundColor Yellow
            }
        }
        
        # Copiar screenshots
        $screenshotDir = Join-Path $repoRoot "TestResults\Screenshots"
        if (Test-Path $screenshotDir) {
            $screenshots = Get-ChildItem -Path $screenshotDir -Filter "*.png" -File
            if ($screenshots.Count -gt 0) {
                Write-Host "  │  ✓ Screenshots: $($screenshots.Count) capturas" -ForegroundColor Green
                $browserScreenshotDir = Join-Path $browserResultDir "screenshots"
                New-Item -ItemType Directory -Force -Path $browserScreenshotDir | Out-Null
                Copy-Item -Path "$screenshotDir\*" -Destination $browserScreenshotDir -Force
            }
        }
        
        Write-Host "  └─ Completado`n" -ForegroundColor DarkGray
        
        # Limpiar variables de entorno
        Remove-Item Env:\ALLURE_RESULTS_DIRECTORY -ErrorAction SilentlyContinue
        Remove-Item Env:\CURRENT_BROWSER -ErrorAction SilentlyContinue
    }
    
    # Resumen de ejecución
    Write-Host "[4/5] Resumen de ejecución:" -ForegroundColor Cyan
    Write-Host "  ┌─────────────────────────────────────" -ForegroundColor DarkGray
    foreach ($browser in $browserResults.Keys) {
        $result = $browserResults[$browser]
        $status = if ($result.Failed -eq 0) { "✓" } else { "⚠" }
        $color = if ($result.Failed -eq 0) { "Green" } else { "Yellow" }
        Write-Host "  │ $status $($browser.ToUpper().PadRight(10)) - $($result.Passed)/$($result.Total) tests" -ForegroundColor $color
    }
    Write-Host "  └─────────────────────────────────────" -ForegroundColor DarkGray
    Write-Host "  Total: $totalTests tests | ✓ $totalPassed | ✗ $totalFailed`n" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Yellow" })
    
    # Validar y generar reporte Allure
    Write-Host "[5/5] Generando reporte Allure..." -ForegroundColor Cyan
    
    $allureFiles = Get-ChildItem -Path $allureFinalDir -Filter "*-result.json" -File -ErrorAction SilentlyContinue
    if ($allureFiles.Count -gt 0) {
        Write-Host "  ✓ Encontrados $($allureFiles.Count) resultados Allure" -ForegroundColor Green
        Write-Host "  Abriendo reporte interactivo...`n" -ForegroundColor Green
        
        try {
            allure serve $allureFinalDir
        } catch {
            Write-Host "  ⚠ Error al abrir Allure. Verifica que esté instalado:" -ForegroundColor Yellow
            Write-Host "    npm install -g allure-commandline`n" -ForegroundColor Gray
        }
    } else {
        Write-Host "  ✗ No se generaron archivos Allure (*-result.json)" -ForegroundColor Red
        Write-Host "  Diagnostico:" -ForegroundColor Yellow
        Write-Host "    - Verifica que Allure.SpecFlowPlugin esté instalado" -ForegroundColor Gray
        Write-Host "    - Revisa specflow.json para plugin Allure" -ForegroundColor Gray
        Write-Host "    - Archivos encontrados en $($allureFinalDir):" -ForegroundColor Gray
        Get-ChildItem -Path $allureFinalDir -File | ForEach-Object {
            Write-Host "      - $($_.Name)" -ForegroundColor DarkGray
        }
    }
    
    Write-Host "`n=== Ejecución completada ===" -ForegroundColor Green
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

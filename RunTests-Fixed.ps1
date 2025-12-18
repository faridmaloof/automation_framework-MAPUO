# MAPUO Test Execution Scripts - PROFESSIONAL VERSION
# Framework: Clean Architecture + Screenplay Pattern
# Lider Tecnico: 30+ años experiencia Dev + QA

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

# Script 8.1: Ejecutar pruebas en múltiples navegadores con Allure (VERSIÓN PROFESIONAL)
function Run-All-Browsers-With-Allure {
    Write-Host "`n=== MAPUO - Ejecucion Multi-Browser con Allure ===" -ForegroundColor Green
    Write-Host "Framework: Clean Architecture + Screenplay Pattern" -ForegroundColor DarkGray
    Write-Host "Lider Tecnico: 30+ años experiencia Dev + QA`n" -ForegroundColor DarkGray
    
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $repoRoot = $PSScriptRoot
    
    # Definir directorio unificado de resultados (single source of truth)
    $testResultsDir = Join-Path $repoRoot "TestResults"
    $allureResultsDir = Join-Path $testResultsDir "allure-results"
    $screenshotsDir = Join-Path $testResultsDir "screenshots"
    $trxResultsDir = Join-Path $testResultsDir "trx"
    
    # Limpiar resultados anteriores
    Write-Host "[1/5] Limpiando resultados anteriores..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue $testResultsDir
    
    # Crear estructura de directorios unificada
    New-Item -ItemType Directory -Force -Path $allureResultsDir | Out-Null
    New-Item -ItemType Directory -Force -Path $screenshotsDir | Out-Null
    New-Item -ItemType Directory -Force -Path $trxResultsDir | Out-Null
    
    # Cargar configuración
    Write-Host "[2/5] Cargando configuracion de navegadores..." -ForegroundColor Cyan
    $configPath = Join-Path $repoRoot "tests\E2E\MAPUO.Tests.E2E\webconfig.json"
    $browsers = @("chromium", "firefox", "webkit")

    if (Test-Path $configPath) {
        try {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.Browsers -and $config.Browsers.Count -gt 0) {
                $browsers = $config.Browsers
                Write-Host "  -> Navegadores configurados: $($browsers -join ', ')" -ForegroundColor Green
            }
        } catch {
            Write-Host "  -> Error cargando config, usando default: $($browsers -join ', ')" -ForegroundColor Yellow
        }
    }

    # Ejecutar pruebas por navegador
    Write-Host "[3/5] Ejecutando pruebas en multiples navegadores...`n" -ForegroundColor Cyan
    
    $totalTests = 0
    $totalPassed = 0
    $totalFailed = 0
    $browserResults = @{}

    foreach ($browser in $browsers) {
        Write-Host "  === Navegador: $($browser.ToUpper()) ===" -ForegroundColor Magenta
        Write-Host "      Configurando ambiente..." -ForegroundColor DarkGray
        
        # Configurar variables de entorno para este navegador
        $env:BROWSER = $browser
        $env:CURRENT_BROWSER = $browser
        $env:HEADLESS = "true"
        $env:TEST_ENV = "CI"
        $env:ALLURE_RESULTS_DIRECTORY = $allureResultsDir
        $env:SCREENSHOTS_ON_FAILURE = "true"
        $env:EVIDENCE_BASE_PATH = $testResultsDir
        
        Write-Host "      Ejecutando tests..." -ForegroundColor DarkGray
        $browserTrxDir = Join-Path $trxResultsDir $browser
        New-Item -ItemType Directory -Force -Path $browserTrxDir | Out-Null
        
        $testOutput = dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj `
            --logger "console;verbosity=minimal" `
            --logger "trx;LogFileName=$browser-$timestamp.trx" `
            --results-directory $browserTrxDir `
            --no-build `
            2>&1
        
        # Parsear resultados
        $tests = 0
        $passed = 0
        $failed = 0
        $testOutput | ForEach-Object {
            if ($_ -match "Total.*:\s*(\d+)") { $tests = [int]$Matches[1] }
            if ($_ -match "Correctas.*:\s*(\d+)") { $passed = [int]$Matches[1] }
            if ($_ -match "Con errores.*:\s*(\d+)") { $failed = [int]$Matches[1] }
            if ($_ -match "Passed.*:\s*(\d+)") { $passed = [int]$Matches[1] }
            if ($_ -match "Failed.*:\s*(\d+)") { $failed = [int]$Matches[1] }
        }
        
        if ($tests -gt 0) {
            $totalTests += $tests
            $totalPassed += $passed
            $totalFailed += $failed
            $browserResults[$browser] = @{
                Total = $tests
                Passed = $passed
                Failed = $failed
            }
            $checkmark = if ($failed -eq 0) { "OK" } else { "WARN" }
            $color = if ($failed -eq 0) { "Green" } else { "Yellow" }
            Write-Host "      Resultados: $passed/$tests - $checkmark" -ForegroundColor $color
        } else {
            Write-Host "      No se pudieron parsear resultados" -ForegroundColor Yellow
        }
        
        # Verificar archivos Allure generados
        $allureFiles = Get-ChildItem -Path $allureResultsDir -Filter "*-result.json" -File -ErrorAction SilentlyContinue
        if ($allureFiles -and $allureFiles.Count -gt 0) {
            Write-Host "      Allure: $($allureFiles.Count) archivos JSON generados" -ForegroundColor Green
        } else {
            Write-Host "      Allure: No se generaron archivos JSON" -ForegroundColor Yellow
        }
        
        # Verificar screenshots
        $screenshots = Get-ChildItem -Path $screenshotsDir -Filter "*.png" -File -ErrorAction SilentlyContinue
        if ($screenshots -and $screenshots.Count -gt 0) {
            Write-Host "      Screenshots: $($screenshots.Count) capturas" -ForegroundColor Green
        }
        
        Write-Host "      Completado`n" -ForegroundColor DarkGray
        
        # Limpiar variables de entorno
        Remove-Item Env:\ALLURE_RESULTS_DIRECTORY -ErrorAction SilentlyContinue
        Remove-Item Env:\CURRENT_BROWSER -ErrorAction SilentlyContinue
        Remove-Item Env:\EVIDENCE_BASE_PATH -ErrorAction SilentlyContinue
    }
    
    # Resumen de ejecución
    Write-Host "[4/5] Resumen de ejecucion:" -ForegroundColor Cyan
    Write-Host "  =========================================" -ForegroundColor DarkGray
    foreach ($browser in $browserResults.Keys) {
        $result = $browserResults[$browser]
        $status = if ($result.Failed -eq 0) { "OK  " } else { "WARN" }
        $color = if ($result.Failed -eq 0) { "Green" } else { "Yellow" }
        Write-Host "  $status $($browser.ToUpper().PadRight(10)) - $($result.Passed)/$($result.Total) tests" -ForegroundColor $color
    }
    Write-Host "  =========================================" -ForegroundColor DarkGray
    Write-Host "  Total: $totalTests tests | OK: $totalPassed | FAIL: $totalFailed`n" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Yellow" })
    
    # Validar y generar reporte Allure
    Write-Host "[5/5] Generando reporte Allure..." -ForegroundColor Cyan
    
    $allureFiles = Get-ChildItem -Path $allureResultsDir -Filter "*-result.json" -File -ErrorAction SilentlyContinue
    if ($allureFiles -and $allureFiles.Count -gt 0) {
        Write-Host "  -> Encontrados $($allureFiles.Count) resultados Allure" -ForegroundColor Green
        Write-Host "  -> Directorio unificado: $allureResultsDir" -ForegroundColor Green
        Write-Host "  -> Abriendo reporte interactivo...`n" -ForegroundColor Green
        
        try {
            allure serve $allureResultsDir
        } catch {
            Write-Host "  -> Error al abrir Allure. Verifica que este instalado:" -ForegroundColor Yellow
            Write-Host "     npm install -g allure-commandline`n" -ForegroundColor Gray
        }
    } else {
        Write-Host "  -> No se generaron archivos Allure (*-result.json)" -ForegroundColor Red
        Write-Host "  -> Intentando convertir TRX a Allure...`n" -ForegroundColor Yellow
        Convert-TrxToAllure -TrxDirectory $trxResultsDir -OutputDirectory $allureResultsDir
    }
    
    Write-Host "`n=== Resultados almacenados en: $testResultsDir ===" -ForegroundColor Green
    Write-Host "  - Allure JSON: $allureResultsDir" -ForegroundColor Gray
    Write-Host "  - Screenshots: $screenshotsDir" -ForegroundColor Gray
    Write-Host "  - TRX files: $trxResultsDir`n" -ForegroundColor Gray
}

# Script 7: Generar y abrir reporte Allure
function Open-Allure-Report {
    Write-Host "Generando reporte Allure..." -ForegroundColor Cyan
    
    if (!(Get-Command allure -ErrorAction SilentlyContinue)) {
        Write-Host "Allure no esta instalado. Instalando..." -ForegroundColor Red
        npm install -g allure-commandline
    }
    
    $allureDir = "TestResults\allure-results"
    if (Test-Path $allureDir) {
        allure serve $allureDir
    } else {
        Write-Host "No se encontro directorio de resultados Allure: $allureDir" -ForegroundColor Red
    }
}

# Script TRX to Allure Converter (OPCION B - BACKUP)
function Convert-TrxToAllure {
    param(
        [Parameter(Mandatory=$true)]
        [string]$TrxDirectory,
        
        [Parameter(Mandatory=$true)]
        [string]$OutputDirectory
    )
    
    Write-Host "  -> Convertidor TRX to Allure (OPCION B - Backup)" -ForegroundColor Cyan
    
    $trxFiles = Get-ChildItem -Path $TrxDirectory -Filter "*.trx" -File -Recurse -ErrorAction SilentlyContinue
    
    if (-not $trxFiles -or $trxFiles.Count -eq 0) {
        Write-Host "  -> No se encontraron archivos TRX para convertir" -ForegroundColor Yellow
        return
    }
    
    Write-Host "  -> Encontrados $($trxFiles.Count) archivos TRX" -ForegroundColor Green
    
    $converted = 0
    foreach ($trxFile in $trxFiles) {
        try {
            Write-Host "  -> Procesando: $($trxFile.Name)" -ForegroundColor Gray
            
            # Leer y parsear TRX XML
            [xml]$trxXml = Get-Content $trxFile.FullName
            
            # Extraer informacion de tests
            $testResults = $trxXml.TestRun.Results.UnitTestResult
            
            if ($testResults) {
                foreach ($testResult in $testResults) {
                    # Crear archivo JSON de Allure para cada test
                    $uuid = [guid]::NewGuid().ToString()
                    $testName = $testResult.testName
                    $outcome = $testResult.outcome
                    $duration = [TimeSpan]::Parse($testResult.duration).TotalMilliseconds
                    $startTime = [DateTimeOffset]::Parse($testResult.startTime).ToUnixTimeMilliseconds()
                    $endTime = [DateTimeOffset]::Parse($testResult.endTime).ToUnixTimeMilliseconds()
                    
                    # Determinar status de Allure
                    $allureStatus = switch ($outcome) {
                        "Passed" { "passed" }
                        "Failed" { "failed" }
                        "NotExecuted" { "skipped" }
                        default { "broken" }
                    }
                    
                    # Crear objeto JSON de Allure
                    $allureResult = @{
                        uuid = $uuid
                        historyId = $uuid
                        testCaseId = $uuid
                        fullName = $testName
                        name = $testName
                        status = $allureStatus
                        time = @{
                            start = $startTime
                            stop = $endTime
                            duration = $duration
                        }
                        labels = @(
                            @{ name = "framework"; value = "SpecFlow" }
                            @{ name = "language"; value = "C#" }
                            @{ name = "resultFormat"; value = "allure2" }
                        )
                        parameters = @()
                        links = @()
                        attachments = @()
                    }
                    
                    # Añadir mensaje de error si fallo
                    if ($outcome -eq "Failed" -and $testResult.Output.ErrorInfo) {
                        $errorMessage = $testResult.Output.ErrorInfo.Message
                        $stackTrace = $testResult.Output.ErrorInfo.StackTrace
                        
                        $allureResult.statusDetails = @{
                            message = $errorMessage
                            trace = $stackTrace
                        }
                    }
                    
                    # Guardar archivo JSON
                    $jsonFileName = "$uuid-result.json"
                    $jsonPath = Join-Path $OutputDirectory $jsonFileName
                    $allureResult | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
                }
                
                $converted++
            }
        }
        catch {
            Write-Host "  -> Error al procesar $($trxFile.Name): $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    if ($converted -gt 0) {
        Write-Host "  -> Conversion completa: $converted archivos TRX convertidos" -ForegroundColor Green
        Write-Host "  -> Abriendo reporte Allure desde archivos convertidos...`n" -ForegroundColor Green
        
        try {
            allure serve $OutputDirectory
        } catch {
            Write-Host "  -> Error al abrir Allure. Verifica instalacion: npm install -g allure-commandline" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  -> No se pudieron convertir archivos TRX" -ForegroundColor Red
    }
}

# Mensaje de ayuda
function Show-Help {
    Write-Host ""
    Write-Host "MAPUO Test Execution Scripts - PROFESSIONAL" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host ""
    
    Write-Host "Comandos disponibles:" -ForegroundColor Yellow
    Write-Host "  Run-E2E-Visible                 - Ejecuta pruebas E2E con navegador visible" -ForegroundColor White
    Write-Host "  Run-E2E-Headless                - Ejecuta pruebas E2E en modo headless" -ForegroundColor White
    Write-Host "  Run-All-Browsers-With-Allure    - Ejecuta multi-browser con reporte Allure" -ForegroundColor White
    Write-Host "  Open-Allure-Report              - Genera y abre reporte Allure" -ForegroundColor White
    Write-Host "  Show-Help                       - Muestra esta ayuda" -ForegroundColor White
    Write-Host ""
}

# Mostrar ayuda al cargar el script
Write-Host ""
Write-Host "MAPUO - Framework Profesional de Automatizacion" -ForegroundColor Green
Write-Host "Ejecuta Show-Help para ver comandos disponibles." -ForegroundColor Cyan
Write-Host ""

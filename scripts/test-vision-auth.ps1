param([Parameter(Mandatory = $true)][string]$AssemblyPath)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $AssemblyPath).Path)
$service = $assembly.GetType('WandEnhancer.Core.Auth.VisionAuthService', $true)
$flags = [Reflection.BindingFlags]'NonPublic, Static'
$friendly = $service.GetMethod('FriendlyMessage', $flags)
$permanent = $service.GetMethod('IsPermanentFailure', $flags)

function Assert-Equal($Actual, $Expected, [string]$Message) {
    if ($Actual -ne $Expected) { throw $Message }
}

function Describe([string]$Message, [int]$Status) {
    $friendly.Invoke($null, [object[]]@($Message, [Net.HttpStatusCode]$Status))
}

# No requests: exercise the compiled production formatter with synthetic responses.
$cases = @(
    @{ Status = 401; Body = ''; Expected = 'Vision Auth: HTTP 401 | autorização recusada. Verifique a configuração do loader.' },
    @{ Status = 403; Body = ''; Expected = 'Vision Auth: HTTP 403 | autorização recusada. Verifique a configuração do loader.' },
    @{ Status = 401; Body = 'invalid loader token'; Expected = 'Vision Auth: HTTP 401 | autenticação do loader recusada.' },
    @{ Status = 403; Body = 'invalid license'; Expected = 'Vision Auth: HTTP 403 | Key inválida.' },
    @{ Status = 400; Body = 'HWID mismatch'; Expected = 'Vision Auth: HTTP 400 | Licença já vinculada a outro HWID ou HWID incompatível.' },
    @{ Status = 403; Body = 'license expired'; Expected = 'Vision Auth: HTTP 403 | Licença expirada.' },
    @{ Status = 403; Body = 'license banned'; Expected = 'Vision Auth: HTTP 403 | Licença bloqueada.' },
    @{ Status = 500; Body = 'Bearer synthetic-private-value licenseKey=synthetic-private-key'; Expected = 'Vision Auth: HTTP 500 | erro no servidor de autenticação.' },
    @{ Status = 429; Body = 'synthetic-private-value'; Expected = 'Vision Auth: HTTP 429 | limite de tentativas. Aguarde e tente novamente.' },
    @{ Status = 400; Body = 'synthetic-private-value synthetic-private-key'; Expected = 'Vision Auth: HTTP 400 | solicitação de licença recusada.' }
)
foreach ($case in $cases) {
    $actual = Describe $case.Body $case.Status
    Assert-Equal $actual $case.Expected "Unexpected safe diagnostic for HTTP $($case.Status)."
    if ($actual.Contains('synthetic-private-')) { throw 'Private response data escaped into the diagnostic.' }
}

foreach ($status in 401, 403) {
    $actual = $permanent.Invoke($null, [object[]]@('', [Net.HttpStatusCode]$status))
    Assert-Equal $actual $false 'Authorization failure must preserve the saved license.'
}
$actual = $permanent.Invoke($null, [object[]]@('invalid loader token', [Net.HttpStatusCode]401))
Assert-Equal $actual $false 'Invalid loader token must preserve the saved license.'
$actual = $permanent.Invoke($null, [object[]]@('invalid license', [Net.HttpStatusCode]403))
Assert-Equal $actual $true 'Explicit invalid license must clear the saved license.'

Write-Host 'Vision Auth diagnostics: 14 cases passed; no network request or real secret used.'

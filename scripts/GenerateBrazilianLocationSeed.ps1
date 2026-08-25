param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'

function Get-DeterministicGuid([string]$value) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $algorithm.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($value))
        [byte[]]$guidBytes = $bytes[0..15]
        return [Guid]::new($guidBytes)
    }
    finally {
        $algorithm.Dispose()
    }
}

function Escape-CSharpString([string]$value) {
    return $value.Replace('\', '\\').Replace('"', '\"')
}

$states = Invoke-RestMethod -Uri 'https://servicodados.ibge.gov.br/api/v1/localidades/estados' |
    Sort-Object sigla

if ($states.Count -ne 27) {
    throw "Expected 27 states but received $($states.Count)."
}

$stateRows = [System.Collections.Generic.List[object]]::new()
$cityRows = [System.Collections.Generic.List[object]]::new()

foreach ($state in $states) {
    $stateId = Get-DeterministicGuid("state:$($state.sigla.ToLowerInvariant())")
    $stateRows.Add([PSCustomObject]@{ Id = $stateId; Name = $state.nome; Abbreviation = $state.sigla })

    $cities = Invoke-RestMethod -Uri "https://servicodados.ibge.gov.br/api/v1/localidades/estados/$($state.id)/municipios" |
        Sort-Object nome

    foreach ($city in $cities) {
        $cityId = Get-DeterministicGuid("city:$($state.sigla.ToLowerInvariant()):$($city.nome.Normalize([Text.NormalizationForm]::FormC).ToLowerInvariant())")
        $cityRows.Add([PSCustomObject]@{ Id = $cityId; StateId = $stateId; Name = $city.nome })
    }
}

if ($cityRows.Count -lt 5000) {
    throw "Expected at least 5000 cities but received $($cityRows.Count)."
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('namespace VoltaMarcada.Infrastructure.Persistence;')
$lines.Add('')
$lines.Add('public sealed record StateSeed(Guid Id, string Name, string Abbreviation);')
$lines.Add('public sealed record CitySeed(Guid Id, Guid StateId, string Name);')
$lines.Add('')
$lines.Add('public static class LocationSeedData')
$lines.Add('{')
$lines.Add('    public static IReadOnlyList<StateSeed> States { get; } =')
$lines.Add('    [')

foreach ($state in $stateRows) {
    $lines.Add("        new(new Guid(`"$($state.Id)`"), `"$(Escape-CSharpString $state.Name)`", `"$($state.Abbreviation)`"),")
}

$lines.Add('    ];')
$lines.Add('')
$lines.Add('    public static IReadOnlyList<CitySeed> Cities { get; } =')
$lines.Add('    [')

foreach ($city in $cityRows) {
    $lines.Add("        new(new Guid(`"$($city.Id)`"), new Guid(`"$($city.StateId)`"), `"$(Escape-CSharpString $city.Name)`"),")
}

$lines.Add('    ];')
$lines.Add('}')

$directory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Path $directory -Force | Out-Null
[IO.File]::WriteAllLines($OutputPath, $lines, [Text.UTF8Encoding]::new($false))

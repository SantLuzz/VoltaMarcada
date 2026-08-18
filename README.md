# ServiceSync

ServiceSync é a fundação de um SaaS B2B multi-tenant para empresas prestadoras de serviços e manutenção. O domínio começa neutro em relação ao nicho de climatização/refrigeração.

## Arquitetura

- `ServiceSync.Domain`: entidades e regras puras de domínio.
- `ServiceSync.Application`: contratos, validação e futuros casos de uso explícitos.
- `ServiceSync.Infrastructure`: EF Core, PostgreSQL, Identity e implementações externas.
- `ServiceSync.Web`: host Blazor Web App com Interactive Server e MudBlazor.

As dependências seguem `Application -> Domain`, `Infrastructure -> Application/Domain` e `Web -> Application/Infrastructure`.

## Stack

.NET 10, ASP.NET Core, Blazor Interactive Server, MudBlazor, EF Core, PostgreSQL/Npgsql, ASP.NET Core Identity e FluentValidation.

## PostgreSQL local

O compose é destinado exclusivamente ao desenvolvimento local e usa credenciais públicas de desenvolvimento:

```powershell
docker compose up -d
```

## Connection string

`src/ServiceSync.Web/appsettings.json` já aponta para o PostgreSQL local do compose. Para outro ambiente, defina uma connection string fora do repositório:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..." --project src/ServiceSync.Web
```

## Migrations

Instale a ferramenta, se necessário:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

Crie a migration inicial:

```powershell
dotnet ef migrations add InitialCreate --project src/ServiceSync.Infrastructure --startup-project src/ServiceSync.Web --output-dir Persistence/Migrations
```

Atualize o banco:

```powershell
dotnet ef database update --project src/ServiceSync.Infrastructure --startup-project src/ServiceSync.Web
```

## Executar

```powershell
dotnet restore
dotnet run --project src/ServiceSync.Web
```

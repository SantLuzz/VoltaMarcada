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

## Administrador da plataforma

O administrador global não é criado pelo cadastro público e não pertence a uma empresa. Configure-o fora do repositório para que a aplicação o provisione e atribua a role `PlatformAdministrator` na inicialização:

```powershell
dotnet user-secrets set "PlatformAdministrator:Email" "admin@exemplo.com" --project src/ServiceSync.Web
dotnet user-secrets set "PlatformAdministrator:Password" "UseUmaSenhaForteAqui" --project src/ServiceSync.Web
```

Em produção, forneça os mesmos valores por variáveis de ambiente ou pelo gerenciador de segredos da hospedagem. Nunca os adicione ao `appsettings.json` versionado.

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

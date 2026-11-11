<p align="center"><img src="src/VoltaMarcada.Web/wwwroot/logos/logo.png" alt="VoltaMarcada" width="220"></p>

# VoltaMarcada

SaaS B2B multi-tenant para empresas prestadoras de serviços e manutenção. Cobre o ciclo completo do atendimento: cliente, equipamento, ordem de serviço, orçamento, agendamento, lembrete de manutenção e novo agendamento.

> **Status:** projeto pessoal, desenvolvido em cerca de um mês (08/2026 a 09/2026) e hoje fora do ar. O código fica aberto como portfólio de arquitetura, testes e desenvolvimento com IA.

## Destaques

- **Desenvolvido inteiramente com IA usando SDD (Spec-Driven Development).** Eu defino produto, arquitetura e regras de negócio em especificações e planos; agentes de IA implementam e verificam cada tarefa. Veja [Como foi desenvolvido](#como-foi-desenvolvido).
- **Multi-tenant**, com isolamento de dados por empresa e autenticação com ASP.NET Core Identity.
- **Clean Architecture** em quatro camadas, com 39 entidades de domínio, 42 migrations e 36 páginas.
- **Mais de 200 testes automatizados** (xUnit), incluindo testes de integração.
- Orçamentos em PDF (QuestPDF), cobrança via Pix (BR Code com QR Code), arquivos no Cloudinary, e-mail por SMTP e links de WhatsApp.
- Deploy automatizado em VPS com Docker, Caddy e GitHub Actions.

## Stack

.NET 10, ASP.NET Core, Blazor Interactive Server, MudBlazor, EF Core, PostgreSQL (Npgsql), ASP.NET Core Identity, FluentValidation, QuestPDF, QRCoder, xUnit, Docker, Caddy e GitHub Actions.

## Arquitetura

| Projeto | Responsabilidade |
| --- | --- |
| `VoltaMarcada.Domain` | Entidades e regras puras de domínio |
| `VoltaMarcada.Application` | Contratos, validação e casos de uso |
| `VoltaMarcada.Infrastructure` | EF Core, PostgreSQL, Identity e integrações externas |
| `VoltaMarcada.Web` | Host Blazor Web App (Interactive Server) com MudBlazor |

As dependências seguem `Application -> Domain`, `Infrastructure -> Application/Domain` e `Web -> Application/Infrastructure`.

## Como foi desenvolvido

O projeto segue SDD: nada é implementado sem especificação e plano escritos antes.

1. O produto e as regras de negócio viram **especificações** em [`docs/superpowers/specs`](docs/superpowers/specs).
2. Cada especificação gera um **plano de tarefas** em [`docs/superpowers/plans`](docs/superpowers/plans).
3. Agentes de IA implementam, revisam e verificam cada tarefa, seguindo as regras do projeto em [`AGENTS.md`](AGENTS.md).

## Executar localmente

Pré-requisitos: .NET 10 SDK e Docker.

```powershell
docker compose up -d
dotnet restore
dotnet run --project src/VoltaMarcada.Web
```

O `docker-compose.yml` sobe um PostgreSQL apenas para desenvolvimento local, com credenciais públicas de desenvolvimento. O `appsettings.json` já aponta para ele.

Para outro ambiente, defina a connection string fora do repositório:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=...;Username=...;Password=..." --project src/VoltaMarcada.Web
```

### Administrador da plataforma

O administrador global não é criado pelo cadastro público e não pertence a uma empresa. Configure-o fora do repositório; a aplicação o provisiona na inicialização com a role `PlatformAdministrator`:

```powershell
dotnet user-secrets set "PlatformAdministrator:Email" "admin@exemplo.com" --project src/VoltaMarcada.Web
dotnet user-secrets set "PlatformAdministrator:Password" "UseUmaSenhaForteAqui" --project src/VoltaMarcada.Web
```

Em produção, use variáveis de ambiente ou o gerenciador de segredos da hospedagem. Nunca adicione esses valores ao `appsettings.json`.

### Migrations

```powershell
dotnet tool install --global dotnet-ef --version 10.*
dotnet ef database update --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web
```

### Testes

```powershell
dotnet test
```

## Documentação adicional

- [Fluxos de ajuda e suporte](docs/help-and-support-flows.md)
- [Fluxos e funcionalidades](docs/fluxos-e-funcionalidades-para-landing.md)

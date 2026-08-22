# ServiceSync

## Plano de Negócio e Desenvolvimento --- Estrutura Atualizada

**Versão:** 2.0\
**Produto:** ServiceSync\
**Modelo:** SaaS B2B para prestadores de serviços técnicos\
**Segmento inicial:** empresas de instalação, manutenção e climatização

------------------------------------------------------------------------

# 1. Direção do produto

O ServiceSync é um SaaS operacional e de recorrência para empresas de
serviços técnicos, começando pelo segmento de climatização.

O produto organiza o ciclo completo do atendimento e transforma serviços
realizados em oportunidades futuras de receita.

O ServiceSync não deve ser posicionado apenas como um sistema de Ordem
de Serviço. A OS é parte do produto; o diferencial está em utilizar o
histórico operacional para trazer clientes de volta no momento adequado.

A proposta central é:

> **Organizar o atendimento de ponta a ponta e transformar serviços
> realizados hoje em oportunidades futuras de atendimento.**

O produto possui dois motores complementares.

## 1.1. Motor operacional

``` text
Captação
→ Orçamento
→ Aprovação
→ OS
→ Agenda
→ Técnico
→ Execução
→ Evidências
→ Conclusão
→ Histórico
```

## 1.2. Motor de recorrência

``` text
Conclusão
→ Próxima manutenção
→ Oportunidade
→ Contato
→ Agendamento
→ Novo atendimento
→ Nova OS
→ Nova previsão
↺
```

O segundo motor representa o principal diferencial comercial, mas
depende de uma operação bem estruturada no primeiro.

------------------------------------------------------------------------

# 2. Fluxo principal do ServiceSync

O fluxo principal permanece:

``` text
Cliente
↓
Equipamento
↓
Ordem de Serviço
↓
Serviço executado
↓
Próxima manutenção
↓
Oportunidade de manutenção
↓
Contato
↓
Link contextualizado de agendamento
↓
Solicitação de horário
↓
Confirmação
↓
Novo atendimento
↓
Nova OS
↓
Nova previsão
↺
```

Além desse fluxo, o sistema deve permitir caminhos alternativos.

Exemplos:

``` text
WhatsApp
→ Cliente
→ OS
```

``` text
WhatsApp
→ Cliente
→ Agendamento
→ OS
```

``` text
Cliente
→ Orçamento
→ Aprovação
→ OS
```

``` text
Link público
→ Solicitação
→ Agendamento
→ OS
```

``` text
Manutenção prevista
→ Oportunidade
→ Contato
→ Link
→ Solicitação
→ Agendamento
→ OS
```

O software deve se adaptar ao processo da empresa e não obrigar a
empresa a seguir um único fluxo.

------------------------------------------------------------------------

# 3. Conceito de oportunidade de manutenção

O conceito de recorrência não deve ser modelado apenas como um lembrete.

Uma manutenção futura representa uma oportunidade comercial.

Conceitualmente:

``` text
MaintenanceOpportunity

Id
CompanyId
CustomerId
EquipmentId
OriginServiceOrderId
RecommendedServiceId
DueDate
Status
ContactStatus
LastContactAt
NextContactAt
CreatedAt
```

A oportunidade permite acompanhar o funil:

``` text
Manutenção prevista
↓
Oportunidade
↓
Contato
↓
Solicitação
↓
Agendamento
↓
Serviço realizado
↓
Receita recuperada
```

Isso permitirá futuramente medir indicadores como:

``` text
67 oportunidades
52 contatos
31 solicitações
27 agendamentos
24 serviços realizados
R$ 8.450 recuperados
```

------------------------------------------------------------------------

# 4. Princípios arquiteturais

## 4.1. Multiempresa

O sistema nasce multiempresa.

As principais entidades pertencem a uma `Company`.

O `CompanyId` não deve ser confiado diretamente ao frontend. A empresa
atual deve ser determinada pelo contexto autenticado.

Conceitos:

``` text
ICurrentUser
ICurrentCompany
```

## 4.2. Usuário e empresa

A relação deve ser conceitualmente:

``` text
User
 ↓
CompanyMember
 ↓
Company
```

Exemplo:

``` text
CompanyMember

CompanyId
UserId
Role
Status
JoinedAt
```

Status possíveis:

``` text
Invited
Active
Disabled
```

Papéis iniciais:

``` text
Owner
Employee
```

Arquitetura preparada para:

``` text
Owner
Administrator
Manager
Attendant
Technician
```

## 4.3. Autorização

Evitar regras espalhadas como:

``` csharp
if (user.Role == Role.Owner)
```

Centralizar autorização para permitir evolução futura para permissões
como:

``` text
CanManageCompany
CanManageEmployees
CanViewFinancial
CanManageSchedule
CanCreateQuote
CanExecuteServiceOrder
CanManageSubscription
```

## 4.4. User, Employee e Professional

Não assumir que usuário autenticado e profissional operacional são
necessariamente a mesma entidade.

-   `User`: identidade que acessa o sistema.
-   `CompanyMember`: vínculo do usuário com uma empresa.
-   `Professional`: recurso operacional que executa serviços e pode
    possuir agenda.

Um `Professional` pode estar associado a um `CompanyMember`, quando
aplicável.

------------------------------------------------------------------------

# 5. Roadmap atualizado

# Fase 1 --- Fundação (Concluída)

## Desenvolver

-   Solution;
-   projetos Domain/Application/Infrastructure/Web;
-   PostgreSQL;
-   EF Core;
-   ASP.NET Core Identity;
-   autenticação;
-   criação de usuário;
-   criação de empresa;
-   multi-tenancy;
-   `ICurrentUser`;
-   `ICurrentCompany`;
-   `CompanyMember`;
-   papéis iniciais `Owner` e `Employee`;
-   base centralizada de autorização.

## Objetivo

O usuário consegue criar conta, cadastrar sua empresa e acessar o
ambiente isolado da empresa com autenticação e autorização adequadas.

------------------------------------------------------------------------

# Fase 2 --- Base operacional

## Clientes

-   cadastro;
-   múltiplos endereços;
-   CPF/CNPJ opcional;
-   telefone;
-   WhatsApp;
-   e-mail;
-   observações.

## Equipamentos

-   cliente;
-   endereço/local;
-   tipo;
-   fabricante;
-   modelo;
-   número de série;
-   capacidade;
-   localização;
-   data de instalação;
-   observações.

## Tipos de serviço

A empresa cadastra os serviços realizados.

Exemplos:

``` text
Instalação
Limpeza
Manutenção preventiva
Manutenção corretiva
Visita técnica
Carga de gás
```

Dados possíveis:

-   nome;
-   descrição;
-   duração aproximada;
-   tipo de preço:
   - preço fixo;
   - a partir de;
   - sob orçamento;
   - não exibir preço.     
-   valor;
-   permitir solicitação online;
-   regras operacionais.

Exemplo:

``` text
Limpeza preventiva

Duração: 120 min
Preço: a partir de R$ 250

Permitir solicitação online: Sim
Checklist: obrigatório
Fotos antes: obrigatórias
Fotos depois: obrigatórias
```

## Profissionais

-   cadastro;
-   vínculo opcional com usuário/CompanyMember;
-   serviços que executa;
-   status.

## Disponibilidade

-   dias de trabalho;
-   horários;
-   intervalos;
-   folgas;
-   férias;
-   compromissos;
-   treinamentos;
-   ausências;
-   bloqueios manuais.

## Infraestrutura já incorporada

-   armazenamento de arquivos via abstração `IFileStorage`;
-   Cloudinary;
-   variáveis de ambiente/secrets;
-   Dockerfile;
-   deploy.

## Objetivo

A empresa consegue configurar a estrutura operacional necessária para
começar a trabalhar.

------------------------------------------------------------------------

# Fase 3 --- Ordem de Serviço e execução

A OS deve nascer preparada para representar a execução real do serviço.

## Ordem de Serviço

Incluir:

-   cliente;
-   equipamento;
-   tipo de serviço;
-   profissional/técnico;
-   prioridade;
-   problema relatado;
-   itens;
-   observações;
-   status;
-   criação direta;
-   conclusão.

## Checklist dinâmico

O checklist pertence ao tipo de serviço e é configurável por empresa.

Estrutura conceitual:

``` text
ServiceType
 ↓
ServiceChecklist
 ↓
ServiceChecklistItem
```

Exemplo:

``` text
Limpeza de ar-condicionado

- Verificar filtros
- Limpar evaporadora
- Verificar condensadora
- Verificar drenagem
- Testar funcionamento
```

Se não existir checklist configurado para o serviço, a OS funciona
normalmente sem checklist.

## Snapshot do checklist

Quando a OS for criada, os itens atuais do template devem ser copiados
para a OS.

``` text
Checklist Template
       ↓
    Snapshot
       ↓
ServiceOrderChecklistItem
```

Conceitualmente:

``` text
ServiceOrderChecklistItem

Id
ServiceOrderId
Description
IsRequired
IsCompleted
CompletedAt
```

Alterações futuras no template não modificam OS antigas.

## Evidências fotográficas

A OS permite:

``` text
Fotos antes
↓
Checklist
↓
Fotos depois
↓
Observações
↓
Finalizar serviço
```

Tipos iniciais:

``` text
Before
After
Other
```

Conceitualmente:

``` text
ServiceOrderPhoto

Id
ServiceOrderId
Type
FileUrl
Description
UploadedByUserId
CreatedAt
```

Não é necessário vincular uma foto a cada item do checklist no MVP.

## Upload

As fotos devem ser persistidas praticamente imediatamente:

``` text
Foto selecionada/tirada
↓
Upload
↓
Storage
↓
ServiceOrderPhoto
↓
Interface exibe foto persistida
```

Evitar manter várias imagens grandes na memória do Blazor Server
aguardando a finalização da OS.

Aplicar limites de tamanho e quantidade. Compressão/redimensionamento
pode evoluir posteriormente.

## Captura pelo celular

Inicialmente utilizar o navegador móvel, por exemplo com `InputFile` ou
input de arquivo com:

``` html
<input type="file"
       accept="image/*"
       capture="environment">
```

Não é necessário criar uma câmera personalizada no MVP.

## Persistência do checklist

Não realizar necessariamente uma requisição por checkbox.

Utilizar uma combinação de:

-   estado local;
-   autosave;
-   atualização em lote;
-   salvar progresso;
-   salvar antes de sair;
-   persistência obrigatória antes da conclusão.

## Validação da conclusão

O backend é responsável pelas regras finais.

Antes de concluir:

``` text
Checklist obrigatório existe?
Todos os itens obrigatórios foram concluídos?

Fotos Before são obrigatórias?
Existem fotos Before?

Fotos After são obrigatórias?
Existem fotos After?
```

Somente após validação:

``` text
InProgress → Completed
```

## Objetivo

A empresa consegue criar e executar uma OS real, com checklist,
evidências e regras de conclusão.

------------------------------------------------------------------------

# Fase 4 --- Agenda e experiência do técnico

## Agenda

Desenvolver:

-   calendário;
-   disponibilidade;
-   cálculo de slots;
-   criação manual;
-   vínculo com OS;
-   múltiplos appointments por OS;
-   bloqueios;
-   reagendamento;
-   histórico de reagendamentos.

Uma OS pode possuir vários compromissos.

Exemplo:

``` text
OS #42

18/08
Visita técnica
Concluída

20/08
Retirada do equipamento
Concluída

25/08
Instalação
Agendada
```

## Dashboard do Owner

O Owner possui visão administrativa e operacional da empresa.

Deve acompanhar:

-   solicitações;
-   orçamentos;
-   OS;
-   agenda;
-   clientes;
-   equipamentos;
-   manutenções;
-   oportunidades;
-   serviços em andamento.

## Dashboard do técnico

O técnico não deve receber apenas o dashboard administrativo com menus
escondidos.

A experiência deve ser mobile-first.

A tela deve responder:

> **Onde preciso ir agora e o que preciso fazer?**

Exemplo:

``` text
Olá, João

HOJE

4 serviços agendados
1 concluído
3 pendentes

PRÓXIMO ATENDIMENTO

10:30
Maria Oliveira
Limpeza preventiva

Samsung Inverter
12.000 BTUs

[ VER ENDEREÇO ]
[ INICIAR ATENDIMENTO ]

AGENDA DE HOJE

✓ 08:00 Carlos Silva
→ 10:30 Maria Oliveira
  14:00 João Santos
  16:30 Empresa ABC
```

O técnico deve acessar principalmente:

-   agenda do dia;
-   próximo atendimento;
-   cliente;
-   endereço;
-   equipamento;
-   OS;
-   checklist;
-   fotos;
-   observações;
-   histórico relevante do equipamento.

## Objetivo

A empresa controla os atendimentos e o técnico possui uma experiência
adequada para executar o trabalho em campo.

------------------------------------------------------------------------

# Fase 5 --- Recorrência e oportunidades

Esta é a fase estratégica central do ServiceSync.

## Próxima manutenção

Ao concluir uma OS:

``` text
Recomendar próxima manutenção?

[ Sim ]

Quando?

30 dias
60 dias
90 dias
180 dias
Personalizado

Serviço recomendado:
Limpeza preventiva
```

O sistema cria uma `MaintenanceOpportunity`.

## Central de oportunidades

Exemplo:

``` text
MANUTENÇÕES

3 vencidas
8 nos próximos 7 dias
21 nos próximos 30 dias
```

Cada oportunidade pode mostrar:

``` text
João Silva
LG Dual Inverter

Última manutenção:
18/05/2026

Próxima recomendada:
18/08/2026

[ WHATSAPP ]
[ AGENDAR ]
[ IGNORAR ]
```

Estados devem representar o ciclo comercial, evitando complexidade
desnecessária.

Exemplo conceitual:

``` text
Pending
DueSoon
Contacted
BookingRequested
Booked
Completed
Dismissed
```

## Objetivo

O ServiceSync deixa de ser apenas um sistema operacional e começa a
demonstrar seu principal diferencial: identificar clientes que podem
gerar novos serviços.

------------------------------------------------------------------------

# Fase 6 --- Contato e histórico de relacionamento

Contato não deve existir apenas como ação visual.

Criar conceito equivalente a:

``` text
CustomerContact

Id
CustomerId
OpportunityId
Channel
Status
CreatedAt
SentAt
Message
```

Canal inicial:

``` text
WhatsAppLink
```

Futuramente:

``` text
WhatsAppCloudApi
```

## WhatsApp no MVP

Contato assistido/manual.

``` text
Oportunidade
↓
Entrar em contato
↓
Mensagem preparada
↓
Abrir WhatsApp
↓
Empresa envia
```

Isso permite registrar a intenção e o histórico sem exigir a API oficial
do WhatsApp.

## Objetivo

Transformar oportunidade em ação comercial rastreável e preparar a
arquitetura para automação futura.

------------------------------------------------------------------------

# Fase 7 --- Agendamento público

## Página pública da empresa

Exemplo:

``` text
servicesync.app/acme-refrigeracao
```

Pode apresentar:

-   logo;
-   empresa;
-   descrição;
-   telefone;
-   WhatsApp;
-   serviços disponíveis;
-   solicitação de atendimento.

Fluxo:

``` text
Serviço
↓
Profissional ou qualquer disponível
↓
Data
↓
Horário
↓
Dados pessoais
↓
Solicitar
```

## Link contextualizado de manutenção

Uma oportunidade pode gerar um token contextualizado.

O sistema já conhece:

-   empresa;
-   cliente;
-   equipamento;
-   serviço recomendado;
-   oportunidade.

Exemplo:

``` text
ACME Refrigeração

Olá, João.

Está chegando o período recomendado para manutenção do seu:

Ar-condicionado LG Dual Inverter

Serviço recomendado:
Limpeza preventiva

Duração aproximada:
2 horas

[ ESCOLHER DIA E HORÁRIO ]
```

## Solicitação de horário

O cliente vê horários compatíveis com:

-   profissional;
-   serviço;
-   duração;
-   expediente;
-   compromissos;
-   bloqueios.

A escolha cria:

``` text
AppointmentRequest
```

e não um agendamento confirmado automaticamente.

## Confirmação

A empresa pode:

``` text
[ CONFIRMAR ]
[ SUGERIR OUTRO HORÁRIO ]
[ RECUSAR ]
```

Após confirmação:

``` text
AppointmentRequest
       ↓
Appointment
```

A oportunidade passa para o estado correspondente a agendamento
confirmado.

## Agendamento manual

O link nunca é obrigatório.

A empresa também pode combinar um horário diretamente com o cliente e
criar o `Appointment` manualmente.

## Objetivo

Fechar digitalmente o ciclo de recorrência:

``` text
Oportunidade
→ contato
→ link
→ cliente escolhe
→ empresa confirma
→ atendimento
```

------------------------------------------------------------------------

# Fase 8 --- Solicitações e orçamentos

## Solicitação

Suportar entrada pública e tratamento interno da solicitação.

Fluxo principal possível:

``` text
Solicitação
↓
Avaliação
↓
Orçamento
↓
Aprovação
↓
OS
```

Sem tornar esse caminho obrigatório.

Também continua válido:

``` text
Cliente
↓
OS direta
```

## Orçamento

Pode conter:

-   serviços;
-   peças;
-   materiais;
-   deslocamento;
-   desconto;
-   observações;
-   validade;
-   total.

Status:

``` text
Draft
Sent
Approved
Rejected
Expired
```

## Orçamento público

Cada orçamento pode gerar um link seguro:

``` text
/q/{token}
```

O cliente pode:

-   visualizar;
-   aprovar;
-   recusar;
-   entrar em contato.

PDF pode existir como recurso complementar, mas o link público é o
principal mecanismo digital.

## WhatsApp

Fluxo:

``` text
Orçamento preparado
↓
Mensagem pronta + link
↓
WhatsApp
↓
Cliente abre
↓
Aprova ou recusa
```

## Objetivo

Eliminar processos externos entre solicitação, diagnóstico, orçamento,
aprovação e criação da OS.

------------------------------------------------------------------------

# Fase 9 --- Histórico, dashboards e refinamento operacional

## Histórico do equipamento

O equipamento deve funcionar como um histórico técnico.

Exemplo:

``` text
LG Dual Inverter
12.000 BTUs
Quarto

18/08/2026

Limpeza preventiva
OS #42

Técnico: João

Checklist:
6/6

Evidências:
3 fotos Before
2 fotos After

Valor:
R$ 250

Próxima manutenção:
18/02/2027
```

## Dashboard do Owner

O dashboard deve responder primeiro:

> **O que precisa da minha atenção hoje?**

Exemplo:

``` text
HOJE

4 atendimentos

MANUTENÇÕES
3 atrasadas
5 precisam de contato esta semana

SOLICITAÇÕES
2 aguardando tratamento

ORÇAMENTOS
3 aguardando resposta
```

Posteriormente deve responder também:

> **Onde existem oportunidades comerciais?**

Exemplo:

``` text
18 clientes estão no período de manutenção

Receita potencial estimada:
R$ X
```

## Métricas de valor

Não medir apenas quantidade de OS.

Priorizar métricas como:

-   oportunidades identificadas;
-   clientes contatados;
-   solicitações originadas por manutenção;
-   agendamentos recuperados;
-   serviços realizados;
-   receita recuperada.

## Objetivo

Transformar dados operacionais em contexto técnico e oportunidades
comerciais.

------------------------------------------------------------------------

# Fase 10 --- SaaS comercial e lançamento

## Cadastro autônomo

Manter o modelo self-service.

``` text
Site
↓
Criar conta
↓
Criar empresa
↓
Trial
↓
Usar ServiceSync
```

A prospecção manual pode direcionar empresas interessadas diretamente
para esse cadastro.

## Convite de funcionários

Fluxo:

``` text
Owner
↓
Funcionários
↓
Convidar funcionário
↓
E-mail
↓
Link
↓
Criar conta/Aceitar
↓
CompanyMember
```

Estrutura conceitual:

``` text
CompanyInvite

CompanyId
Email
Role
Token
ExpiresAt
AcceptedAt
CreatedAt
```

## Trial

Estratégia inicial:

``` text
14 dias
```

O período deve ser validado comercialmente com os primeiros clientes.

## Assinatura

A assinatura pertence à empresa:

``` text
Company
   ↓
Subscription
```

e não diretamente ao usuário.

## Plano inicial

Preço inicial de referência:

``` text
R$ 49,90/mês
```

O preço deve ser validado com clientes reais.

## Admin do ServiceSync

O administrador do SaaS deve conseguir acompanhar:

-   empresas;
-   trials;
-   empresas ativas;
-   vencidas;
-   canceladas;
-   assinaturas;
-   planos;
-   usuários;
-   uso do sistema.

A cobrança pode começar com operação simples/manual e evoluir para
integração com gateway conforme a base de clientes justificar a
automação.

## Objetivo

Transformar a aplicação funcional em um SaaS comercializável e
administrável.

------------------------------------------------------------------------

# 6. Definição do MVP completo

O primeiro MVP comercial deve entregar a primeira versão completa da
proposta ServiceSync.

``` text
EMPRESA
   ↓
Cadastro / Trial
   ↓
Clientes
   ↓
Equipamentos
   ↓
Serviços
   ↓
Equipe
   ↓
Solicitação ───────────────┐
   ↓                       │
Orçamento                  │
   ↓                       │
Aprovação                  │
   ↓                       │
OS ←───────────────────────┘
   ↓
Agenda
   ↓
Técnico
   ↓
Fotos Before
   ↓
Checklist
   ↓
Fotos After
   ↓
Conclusão
   ↓
Histórico do equipamento
   ↓
Próxima manutenção
   ↓
MaintenanceOpportunity
   ↓
Contato
   ↓
WhatsApp
   ↓
Link contextualizado
   ↓
Cliente escolhe horário
   ↓
AppointmentRequest
   ↓
Empresa confirma
   ↓
Appointment
   ↓
Nova OS
   ↓
Novo serviço
   ↓
Nova oportunidade
   ↺
```

Esse MVP é maior que um produto mínimo extremamente reduzido, mas
permanece concentrado em um único ciclo de valor.

------------------------------------------------------------------------

# 7. O que fica fora do MVP

Não desenvolver agora:

-   estoque completo;
-   controle complexo de peças;
-   financeiro completo;
-   contas a pagar/receber;
-   emissão fiscal;
-   NFS-e;
-   contabilidade;
-   folha;
-   PMOC;
-   contratos recorrentes;
-   roteirização;
-   GPS;
-   aplicativo mobile nativo;
-   chat interno;
-   IA;
-   CRM genérico/complexo;
-   marketplace;
-   pagamento online do cliente final;
-   API oficial do WhatsApp;
-   automações complexas;
-   permissões extremamente granulares;
-   câmera personalizada;
-   foto obrigatória por item do checklist;
-   geolocalização das evidências;
-   assinatura digital avançada;
-   QR Code no equipamento;
-   relatórios avançados.

A arquitetura pode considerar evolução futura, mas essas funcionalidades
não devem bloquear a validação do ciclo principal.

------------------------------------------------------------------------

# 8. Estrutura futura de planos

## Start

Referência inicial:

**R\$ 49,90/mês**

Core do ServiceSync:

-   solicitações;
-   clientes;
-   equipamentos;
-   serviços;
-   orçamentos;
-   aprovação;
-   OS;
-   agenda;
-   equipe;
-   checklist;
-   evidências;
-   histórico;
-   próxima manutenção;
-   oportunidades;
-   contato manual pelo WhatsApp;
-   agendamento público.

## Pro

Preço futuro a validar.

Possíveis diferenciais:

-   WhatsApp oficial;
-   automação de recorrência;
-   régua de manutenção;
-   mais funcionários;
-   dashboard avançado;
-   financeiro;
-   relatórios;
-   automações;
-   controles operacionais adicionais.

Exemplo de régua:

``` text
5 dias antes
↓
WhatsApp automático

Data prevista
↓
Segundo contato

15 dias depois
↓
Follow-up
```

O posicionamento deve ser:

> **Automação de recorrência**

ou:

> **Recuperação automática de manutenções**

e não simplesmente "disparo automático".

## Business

Preço futuro a validar.

Possíveis recursos:

-   contratos;
-   equipes maiores;
-   permissões avançadas;
-   estoque;
-   gestão avançada de equipamentos;
-   PMOC;
-   relatórios profissionais;
-   indicadores;
-   recursos B2B avançados.

Os limites de usuários e preços não devem ser definidos definitivamente
antes da validação comercial.

------------------------------------------------------------------------

# 9. Estratégia de validação

Primeira meta:

``` text
1 cliente piloto
↓
aproximadamente 10 empresas
↓
validar uso e disposição a pagar
```

A estratégia inicial deve ser prospecção manual de empresas de
climatização.

Perguntas importantes durante a validação:

-   Quantos clientes antigos deixam de ser chamados?
-   Como a empresa sabe quando um equipamento precisa de manutenção
    novamente?
-   Quem controla isso atualmente?
-   Utilizam agenda, papel, planilha ou WhatsApp?
-   O cliente utiliza o link de agendamento?
-   O técnico utiliza checklist e evidências sem atrito?
-   Quantas oportunidades o ServiceSync identificou?
-   Quantos clientes foram contatados?
-   Quantos retornaram?
-   Quanto vale recuperar dois ou três serviços por mês?
-   A empresa pagaria para continuar usando o sistema?

O roadmap posterior deve ser orientado principalmente pelo comportamento
e pelas solicitações repetidas dos clientes reais.

------------------------------------------------------------------------

# 10. Métrica principal

A métrica principal não deve ser apenas:

> Quantas OS foram criadas?

O ServiceSync deve conseguir responder:

> **Quantos novos atendimentos foram gerados através das oportunidades
> de manutenção?**

Funil desejado:

``` text
Oportunidades
↓
Contatos
↓
Solicitações
↓
Agendamentos
↓
Serviços realizados
↓
Receita recuperada
```

Essa métrica demonstra o retorno financeiro do produto.

------------------------------------------------------------------------

# 11. Posicionamento

Evitar:

> Sistema de Ordem de Serviço.

Preferir mensagens relacionadas ao resultado:

> **ServiceSync organiza seus atendimentos e ajuda sua empresa a trazer
> seus clientes de volta no momento certo.**

Ou:

> **Faça o serviço hoje. O ServiceSync lembra quando é hora de trazer o
> cliente de volta.**

A OS é uma funcionalidade.

A recorrência é o diferencial.

------------------------------------------------------------------------

# 12. Regra para decisões futuras

Toda nova funcionalidade deve responder pelo menos uma destas perguntas:

1.  Ajuda a empresa a executar melhor seus serviços?
2.  Ajuda a organizar clientes e equipamentos?
3.  Ajuda a recuperar clientes?
4.  Ajuda a transformar uma manutenção prevista em novo atendimento?
5.  Reduz trabalho administrativo do prestador?
6.  É necessária para fechar corretamente o ciclo principal do
    ServiceSync?

Se não responder positivamente a nenhuma delas, provavelmente não deve
entrar no MVP.

------------------------------------------------------------------------

# 13. Princípio final

O ServiceSync deve funcionar como a memória operacional e comercial da
empresa.

A empresa não precisa lembrar:

> "Já deve estar na hora de ligar para aquele cliente."

O ServiceSync sabe.

O sistema informa:

> **Você possui clientes que precisam de manutenção esta semana.**

A empresa entra em contato.

O cliente agenda.

O serviço é executado.

Uma nova previsão é criada.

E o ciclo recomeça.

O princípio que deve orientar produto, arquitetura e desenvolvimento é:

> **Arquitetura preparada para crescer, produto focado em fechar muito
> bem o ciclo principal antes de expandir para um ERP completo.**

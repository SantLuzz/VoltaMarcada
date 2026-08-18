Abaixo está um plano de negócio que também funciona como **documento norteador do desenvolvimento**: sempre que surgir uma feature nova, você pode verificar se ela fortalece o ciclo principal do ServiceSync ou se é algo para depois do MVP. 

# ServiceSync
## Plano de Negócio e Desenvolvimento do Produto

**Versão:** 1.0  
**Produto:** ServiceSync  
**Modelo:** SaaS B2B para prestadores de serviços técnicos

---

# 1. Resumo executivo

O **ServiceSync** é uma plataforma SaaS voltada para empresas e profissionais que realizam serviços técnicos, manutenções, instalações e atendimentos recorrentes.

O sistema nasce para resolver um problema simples, mas de alto impacto:

> Empresas realizam serviços nos clientes, porém frequentemente não possuem um processo organizado para saber quando aquele equipamento precisará de manutenção novamente e acabam perdendo oportunidades de retorno.

O ServiceSync não será apenas um sistema de Ordem de Serviço.

Seu objetivo será acompanhar todo o ciclo de relacionamento entre empresa, cliente e equipamento:

**Cliente → Equipamento → Serviço → OS → Próxima manutenção → Lembrete → Contato → Agendamento → Nova OS**

O sistema permitirá que a empresa saiba quais clientes precisam ser contatados, envie um link para que eles escolham um horário disponível e transforme esse retorno em um novo atendimento.

Além disso, continuará atendendo aos fluxos tradicionais: clientes que chegam por WhatsApp, telefone, indicação ou presencialmente poderão ser cadastrados manualmente e ter orçamento, agendamento ou OS criados diretamente.

---

# 2. Problema

Pequenas empresas de serviços frequentemente trabalham utilizando uma combinação de:

- WhatsApp;
- agenda pessoal;
- papel;
- planilhas;
- memória do proprietário;
- sistemas simples de OS.

O serviço é executado e o cliente vai embora.

Por exemplo:

> João realizou a limpeza do ar-condicionado em agosto.

O técnico sabe que seria interessante realizar outra manutenção aproximadamente três ou seis meses depois.

Porém, essa informação não necessariamente gera uma ação.

Quando chega novembro ou fevereiro, ninguém lembra de entrar em contato com João.

O resultado é uma oportunidade perdida.

O cliente somente procura novamente a empresa quando percebe algum problema — e pode inclusive procurar outro profissional.

---

# 3. Proposta de valor

A principal proposta do ServiceSync será:

> **Transformar serviços realizados hoje em oportunidades futuras de atendimento.**

Ao finalizar uma OS, o profissional poderá informar:

**Próxima manutenção recomendada: 90 dias.**

O ServiceSync passa a acompanhar essa previsão.

Quando a data estiver próxima, o sistema informa à empresa:

> João possui uma manutenção prevista para daqui a 7 dias.

A empresa poderá entrar em contato com João e enviar um link personalizado.

João abre o link, visualiza o serviço recomendado e escolhe entre os horários disponíveis.

A empresa confirma.

Um novo atendimento é criado.

Depois da execução, uma nova previsão pode ser cadastrada.

Isso cria um ciclo recorrente.

---

# 4. O ciclo ServiceSync

O principal ciclo do produto será:

```text
CLIENTE
   ↓
EQUIPAMENTO
   ↓
ORDEM DE SERVIÇO
   ↓
SERVIÇO EXECUTADO
   ↓
PRÓXIMA MANUTENÇÃO
   ↓
LEMBRETE
   ↓
CONTATO COM CLIENTE
   ↓
LINK DE AGENDAMENTO
   ↓
SOLICITAÇÃO
   ↓
CONFIRMAÇÃO
   ↓
NOVO ATENDIMENTO
   ↓
NOVA OS
   ↓
NOVA PREVISÃO
   ↺
```

Este deverá ser considerado o **Core Business do ServiceSync**.

---

# 5. Público-alvo inicial

O produto deve começar atendendo empresas com forte característica de manutenção recorrente.

### Segmento inicial recomendado

Empresas de:

- refrigeração;
- ar-condicionado;
- assistência técnica;
- manutenção de eletrodomésticos;
- equipamentos comerciais;
- elétrica;
- energia solar;
- manutenção de piscinas;
- informática;
- máquinas e equipamentos.

O segmento de refrigeração/ar-condicionado é especialmente interessante para validação porque possui:

- equipamentos identificáveis;
- histórico de manutenção;
- manutenção preventiva;
- visitas técnicas;
- orçamento;
- instalação;
- retorno;
- serviços recorrentes.

Após validar o modelo, o produto poderá ser adaptado para outros prestadores.

---

# 6. Princípio fundamental do produto

O ServiceSync **não deverá obrigar a empresa a seguir um único fluxo**.

Nem todo cliente chegará pelo site.

Nem todo atendimento terá orçamento.

Nem toda OS terá agendamento.

Nem todo agendamento terá vindo de uma solicitação pública.

Portanto, os módulos deverão ser independentes e conectáveis.

Exemplos válidos:

```text
WhatsApp
→ Cliente
→ OS
```

ou:

```text
WhatsApp
→ Cliente
→ Agendamento
→ OS
```

ou:

```text
Cliente
→ Orçamento
→ Aprovação
→ OS
```

ou:

```text
Link público
→ Solicitação
→ Agendamento
→ OS
```

ou:

```text
Manutenção prevista
→ Contato
→ Link
→ Solicitação
→ Agendamento
→ OS
```

O software deve se adaptar ao processo da empresa, e não obrigar a empresa a se adaptar ao software.

---

# 7. Estrutura principal do domínio

O domínio inicial será composto por:

## Empresa

Representa o tenant do SaaS.

Cada empresa possui seus próprios:

- usuários;
- clientes;
- equipamentos;
- profissionais;
- serviços;
- agendas;
- orçamentos;
- ordens de serviço;
- previsões de manutenção.

Todos os dados deverão possuir isolamento por empresa.

---

# 8. Usuários e autenticação

O primeiro fluxo do sistema será:

```text
Criar conta
↓
Criar empresa
↓
Configurar empresa
↓
Acessar dashboard
```

Dados iniciais:

### Usuário

- Nome
- E-mail
- Senha

### Empresa

- Nome/Razão Social
- Nome fantasia
- CNPJ/CPF
- Telefone
- WhatsApp
- Cidade
- Estado
- Logo
- Slug público

Exemplo de slug:

`acme-refrigeracao`

permitindo futuramente uma página pública como:

`servicesync.app/acme-refrigeracao`

### Perfis iniciais

- Owner
- Employee

Futuramente:

- Administrator
- Technician
- Receptionist
- Financial

---

# 9. Clientes

Clientes poderão ser criados manualmente ou automaticamente através de uma solicitação.

Dados:

- Nome
- CPF/CNPJ opcional
- Telefone
- WhatsApp
- E-mail
- Observações

O cliente poderá possuir vários endereços.

Exemplo:

```text
João da Silva

├── Residência
├── Empresa
└── Casa de praia
```

---

# 10. Equipamentos

Equipamentos serão uma parte importante do ServiceSync.

Um cliente poderá possuir vários equipamentos.

Exemplo:

```text
João da Silva

├── Ar-condicionado LG — Quarto
├── Ar-condicionado Samsung — Sala
└── Freezer Metalfrio — Empresa
```

Informações possíveis:

- tipo;
- fabricante;
- modelo;
- número de série;
- capacidade;
- localização;
- data de instalação;
- observações.

Cada equipamento deverá possuir histórico próprio.

---

# 11. Histórico do equipamento

A página do equipamento deverá mostrar sua linha do tempo.

Exemplo:

```text
LG Dual Inverter
12.000 BTUs
Quarto

18/08/2026
Limpeza preventiva
OS #000042
R$ 250

10/02/2026
Manutenção
OS #000017
R$ 380

05/08/2025
Instalação
OS #000003
```

Isso permitirá ao técnico consultar rapidamente tudo que já aconteceu naquele equipamento.

---

# 12. Serviços

A empresa poderá cadastrar seus serviços.

Exemplo:

### Limpeza de ar-condicionado

Duração aproximada: 120 minutos

Preço:

**A partir de R$ 250**

### Instalação

Duração aproximada: 180 minutos

Preço:

**Sob orçamento**

Tipos de preço:

- preço fixo;
- a partir de;
- sob orçamento;
- não exibir preço.

Também deverá existir:

**Permitir solicitação online: Sim/Não**

---

# 13. Profissionais

Cada empresa poderá cadastrar profissionais.

Exemplo:

```text
Carlos
João
Pedro
```

Cada profissional poderá possuir serviços específicos.

Exemplo:

```text
Carlos
├── Instalação
├── Manutenção
└── Limpeza

João
├── Manutenção
└── Limpeza
```

---

# 14. Disponibilidade

Cada profissional possuirá sua disponibilidade.

Exemplo:

```text
Segunda a sexta

08:00–12:00
13:00–18:00
```

Também deverão existir bloqueios:

- folga;
- férias;
- compromisso;
- treinamento;
- ausência;
- bloqueio manual.

---

# 15. Agenda

A agenda será responsável por compromissos reais.

Um agendamento poderá ser criado:

- manualmente;
- a partir de uma OS;
- a partir de uma solicitação pública;
- a partir de uma manutenção prevista.

Um agendamento deverá possuir:

- cliente;
- profissional;
- serviço;
- equipamento opcional;
- OS opcional;
- data;
- horário inicial;
- horário final;
- status.

---

# 16. Múltiplos agendamentos por OS

Uma OS poderá possuir vários agendamentos.

Exemplo:

### OS #42

```text
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

Isso permitirá representar corretamente serviços que exigem retornos.

---

# 17. Reagendamento

Um compromisso existente poderá ser reagendado.

Exemplo:

```text
18/08 — 14:00
        ↓
19/08 — 10:00
```

O ServiceSync deverá guardar histórico.

### Histórico

```text
18/08 — 08:32

Reagendado

De:
18/08 às 14:00

Para:
19/08 às 10:00

Motivo:
Cliente não estará no local.
```

Dessa maneira nenhuma informação operacional será perdida.

---

# 18. Ordem de Serviço

A OS será o centro operacional do atendimento.

Ela poderá ser criada diretamente.

Exemplo:

O cliente chama pelo WhatsApp:

> Meu ar-condicionado parou de gelar.

O funcionário poderá imediatamente:

```text
+ Nova OS
```

Selecionar ou cadastrar o cliente e informar:

- equipamento;
- problema relatado;
- serviço;
- profissional;
- prioridade;
- observações.

Não será necessário passar pelo fluxo público.

---

# 19. Orçamento

Uma OS poderá possuir orçamento, mas isso não será obrigatório.

O orçamento poderá conter:

- serviços;
- peças;
- materiais;
- deslocamento;
- desconto;
- observações;
- validade;
- total.

Status:

- Rascunho
- Enviado
- Aprovado
- Recusado
- Expirado

---

# 20. Orçamento público

Cada orçamento poderá gerar um link seguro.

Exemplo:

`servicesync.app/q/{token}`

O cliente poderá:

- visualizar;
- baixar PDF;
- aprovar;
- recusar;
- entrar em contato.

Isso reduz a dependência do envio de arquivos.

O PDF continuará disponível.

---

# 21. WhatsApp

No MVP não será necessária integração automática com a API oficial.

O ServiceSync poderá possuir botões como:

**Enviar orçamento pelo WhatsApp**

**Lembrar manutenção pelo WhatsApp**

**Enviar link de agendamento**

O sistema abre o WhatsApp com uma mensagem previamente preenchida.

Isso reduz custo e complexidade inicial.

---

# 22. Manutenção prevista

Esta será uma das principais funcionalidades do ServiceSync.

Ao finalizar uma OS:

```text
Recomendar próxima manutenção?

[ Sim ]

Quando?

○ 30 dias
○ 60 dias
● 90 dias
○ 180 dias
○ Personalizado

Serviço recomendado:

Limpeza preventiva
```

O sistema cria uma previsão.

Informações:

- cliente;
- equipamento;
- OS de origem;
- serviço recomendado;
- data prevista;
- observações;
- status.

---

# 23. Status da manutenção prevista

Estados sugeridos:

```text
Pending
DueSoon
Contacted
BookingRequested
Booked
Completed
Dismissed
```

Exemplo:

```text
Pending
↓
DueSoon
↓
Contacted
↓
BookingRequested
↓
Booked
↓
Completed
```

---

# 24. Central de manutenções

O dashboard deverá possuir uma área de destaque:

## Manutenções previstas

```text
Atrasadas               3
Próximos 7 dias          8
Próximos 30 dias        21
```

Exemplo:

### 🔴 João da Silva

Ar-condicionado LG

Previsto: 15/08

**3 dias atrasado**

`Contatar cliente`

---

### 🟠 Maria Souza

Freezer Metalfrio

Previsto: 21/08

**Faltam 3 dias**

`Contatar cliente`

---

Essa tela representa oportunidades comerciais.

Não é simplesmente um lembrete.

O significado para a empresa é:

> **Clientes que podem gerar novos serviços.**

---

# 25. Contato de manutenção

Quando chegar próximo da manutenção:

```text
João da Silva

LG Dual Inverter

Última manutenção:
18/05/2026

Próxima recomendada:
18/08/2026

[ WhatsApp ]
[ Enviar link de agendamento ]
[ Agendar manualmente ]
[ Ignorar ]
```

A empresa decide o que fazer.

---

# 26. Link contextualizado

O link enviado para uma manutenção não deverá ser simplesmente o link genérico da empresa.

O ServiceSync poderá gerar um token contextualizado.

O sistema já conhecerá:

- empresa;
- cliente;
- equipamento;
- serviço recomendado;
- previsão de manutenção.

O cliente encontrará algo como:

# ACME Refrigeração

Olá, João.

Está chegando o período recomendado para manutenção do seu:

**Ar-condicionado LG Dual Inverter**

Serviço recomendado:

**Limpeza preventiva**

Duração aproximada:

**2 horas**

`Escolher dia e horário`

---

# 27. Solicitação de agendamento

O cliente verá somente horários realmente compatíveis com:

- profissional;
- serviço;
- duração;
- expediente;
- compromissos existentes;
- bloqueios.

Exemplo:

```text
Quarta-feira — 19/08

08:00
10:00
13:00
15:00
```

O cliente escolhe 10:00.

Isso gera uma:

**AppointmentRequest**

Não necessariamente um agendamento confirmado.

---

# 28. Confirmação

A empresa recebe:

```text
Nova solicitação

João da Silva

Limpeza preventiva
LG Dual Inverter

19/08
10:00–12:00

[ Confirmar ]
[ Sugerir outro horário ]
[ Recusar ]
```

Depois da confirmação:

```text
AppointmentRequest
        ↓
Appointment
```

E a manutenção prevista passa para:

```text
Booked
```

---

# 29. Agendamento manual

O link nunca deverá ser obrigatório.

Se a empresa ligar:

> João, tenho quinta às 14h. Pode ser?

E João aceitar, o funcionário simplesmente utiliza:

**Agendar manualmente**

A manutenção prevista será vinculada ao novo Appointment.

---

# 30. Página pública genérica

Além dos links de manutenção, cada empresa terá sua página.

Exemplo:

`servicesync.app/acme-refrigeracao`

Ela poderá apresentar:

- logo;
- empresa;
- descrição;
- telefone;
- WhatsApp;
- serviços disponíveis;
- solicitação de atendimento.

O fluxo será:

```text
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

Isso cria uma segunda porta de entrada para novos clientes.

---

# 31. Dashboard

O dashboard não deverá focar apenas em números financeiros.

Deverá responder:

> **O que eu preciso fazer hoje?**

Exemplo:

```text
Bom dia!

HOJE

4 atendimentos

MANUTENÇÕES

3 atrasadas
5 precisam de contato esta semana

SOLICITAÇÕES

2 aguardando confirmação

ORÇAMENTOS

3 aguardando resposta
```

Isso transforma o dashboard em uma central operacional.

---

# 32. Modelo conceitual

```text
Company
 │
 ├── Users
 │
 ├── Customers
 │      │
 │      └── Equipments
 │              │
 │              ├── WorkOrders
 │              │
 │              └── MaintenanceReminders
 │
 ├── Professionals
 │      └── Availability
 │
 ├── Services
 │
 ├── Appointments
 │      └── RescheduleHistory
 │
 ├── AppointmentRequests
 │
 ├── Quotes
 │
 └── WorkOrders
```

---

# 33. Arquitetura técnica

Stack inicial recomendada:

- .NET 10
- Blazor Web App
- ASP.NET Core
- EF Core
- PostgreSQL
- ASP.NET Core Identity

Arquitetura:

```text
ServiceSync.Domain

ServiceSync.Application

ServiceSync.Infrastructure

ServiceSync.Web
```

### Domain

Responsável por:

- entidades;
- enums;
- value objects;
- regras de domínio.

### Application

Responsável pelos Use Cases.

Exemplo:

```text
CreateCustomer
CreateEquipment
CreateWorkOrder
CompleteWorkOrder

ScheduleAppointment
RescheduleAppointment

CreateMaintenanceReminder
GetUpcomingMaintenance
MarkCustomerAsContacted

CreateAppointmentRequest
AcceptAppointmentRequest

CreateQuote
ApproveQuote
```

### Infrastructure

Responsável por:

- EF Core;
- PostgreSQL;
- Identity;
- persistência;
- geração de PDF;
- armazenamento;
- serviços externos.

### Web

Responsável por:

- Blazor;
- páginas;
- componentes;
- endpoints;
- autenticação;
- experiência do usuário.

---

# 34. Multi-tenancy

O sistema deverá nascer multiempresa.

Todas as principais entidades terão:

```text
CompanyId
```

O `CompanyId` jamais deverá ser confiado diretamente ao frontend.

A empresa atual deverá ser obtida através do usuário autenticado.

Conceitos recomendados:

```text
ICurrentUser
ICurrentCompany
```

Essa decisão deve existir desde o primeiro commit relevante do projeto.

---

# 35. Plano de desenvolvimento

## Fase 1 — Fundação

Desenvolver:

1. Solution;
2. projetos Domain/Application/Infrastructure/Web;
3. PostgreSQL;
4. EF Core;
5. Identity;
6. autenticação;
7. criação de usuário;
8. criação de empresa;
9. multi-tenancy;
10. `ICurrentUser`;
11. `ICurrentCompany`.

### Objetivo

Usuário consegue criar uma conta, cadastrar sua empresa e acessar o dashboard.

---

# 36. Fase 2 — Base operacional

Desenvolver:

- clientes;
- endereços;
- equipamentos;
- profissionais;
- serviços;
- disponibilidade.

### Objetivo

Empresa consegue configurar toda a estrutura necessária para trabalhar.

---

# 37. Fase 3 — OS

Desenvolver:

- criação manual de OS;
- vínculo com cliente;
- vínculo com equipamento;
- vínculo com profissional;
- itens;
- status;
- observações;
- conclusão.

### Objetivo

Empresa já consegue utilizar ServiceSync como sistema básico de OS.

---

# 38. Fase 4 — Agenda

Desenvolver:

- calendário;
- disponibilidade;
- cálculo de slots;
- criação manual;
- múltiplos appointments por OS;
- bloqueios;
- reagendamento;
- histórico de reagendamentos.

### Objetivo

Empresa passa a controlar atendimentos através do ServiceSync.

---

# 39. Fase 5 — Core de recorrência

Esta é a fase estratégica mais importante.

Desenvolver:

- previsão de manutenção;
- prazo em dias;
- data específica;
- serviço recomendado;
- vínculo com equipamento;
- dashboard de manutenções;
- filtros;
- atrasadas;
- próximas;
- contatadas;
- histórico.

### Objetivo

ServiceSync passa a lembrar a empresa de oportunidades futuras.

Aqui o produto começa realmente a demonstrar seu diferencial.

---

# 40. Fase 6 — Agendamento público

Desenvolver:

- página pública;
- slug;
- serviços públicos;
- disponibilidade;
- solicitação;
- confirmação;
- recusa;
- sugestão de outro horário.

Depois:

- link contextualizado de manutenção;
- token seguro;
- dados pré-preenchidos;
- vínculo com MaintenanceReminder.

### Objetivo

Fechar o ciclo:

```text
Lembrete
→ contato
→ link
→ cliente escolhe
→ empresa confirma
→ atendimento
```

---

# 41. Fase 7 — Orçamentos

Desenvolver:

- orçamento;
- itens;
- valores;
- descontos;
- validade;
- status;
- PDF;
- link público;
- aprovação;
- recusa;
- WhatsApp.

### Objetivo

Eliminar processos externos entre diagnóstico e aprovação.

---

# 42. Fase 8 — Melhorias da OS

Adicionar:

- fotos;
- antes/depois;
- anexos;
- assinatura simples;
- histórico completo;
- observações internas;
- impressão/PDF.

---

# 43. O que NÃO desenvolver no MVP

Evitar inicialmente:

- estoque complexo;
- emissão fiscal;
- NFS-e;
- contabilidade;
- contas a pagar;
- folha;
- roteirização;
- aplicativo mobile nativo;
- chat interno;
- integração oficial WhatsApp;
- IA;
- CRM complexo;
- pagamento online;
- marketplace.

O objetivo não é construir um ERP inteiro.

---

# 44. MVP comercial

Considerar o primeiro MVP comercial pronto quando for possível executar:

```text
Empresa cria conta
↓
Cadastra cliente
↓
Cadastra equipamento
↓
Cria OS
↓
Executa serviço
↓
Finaliza
↓
Informa próxima manutenção
↓
ServiceSync lembra
↓
Empresa contata cliente
↓
Envia link
↓
Cliente solicita horário
↓
Empresa confirma
↓
Novo atendimento
```

Se esse fluxo funcionar bem, o principal conceito do produto estará validado.

---

# 45. Modelo de monetização

O modelo deverá ser assinatura mensal.

Sugestão inicial de estrutura:

### Teste

**30 dias grátis**

Permitir que a empresa utilize o fluxo real antes de decidir pagar.

### Plano Inicial

Voltado para profissional autônomo.

Exemplo:

- 1 usuário;
- clientes;
- equipamentos;
- OS;
- manutenção recorrente;
- agenda;
- página pública.

### Plano Empresa

Voltado para pequenas equipes.

- múltiplos profissionais;
- múltiplos usuários;
- agendas individuais;
- orçamentos;
- relatórios;
- recursos adicionais.

Os preços deverão ser validados com clientes reais antes de serem definidos definitivamente.

---

# 46. Estratégia de validação

O produto não deverá ser desenvolvido por meses antes de ser colocado diante de empresas reais.

O ideal será selecionar inicialmente algumas empresas de refrigeração/manutenção e acompanhar seu uso.

Perguntas importantes:

**Quantos clientes antigos eles deixam de chamar?**

**Como sabem quando um equipamento precisa de nova manutenção?**

**Quem controla isso atualmente?**

**Usam agenda, papel ou WhatsApp?**

**O cliente aceitaria escolher um horário através de um link?**

**Quanto vale recuperar apenas dois ou três serviços por mês?**

A resposta dessas perguntas deverá orientar o roadmap.

---

# 47. Métrica principal do ServiceSync

Uma das métricas mais importantes não deverá ser simplesmente:

> Quantas OS foram criadas?

Deveremos medir:

> **Quantos novos atendimentos foram gerados através de manutenções previstas?**

Exemplo:

```text
Agosto

67 manutenções previstas

52 clientes contatados

31 solicitações

27 agendamentos

24 serviços realizados

R$ 8.450 recuperados
```

Isso demonstra claramente valor financeiro.

---

# 48. Visão futura do dashboard

Uma futura seção poderá mostrar:

## Receita recuperada pelo ServiceSync

```text
Este mês

R$ 8.450

24 serviços realizados
através de lembretes de manutenção
```

Esse número pode ser extremamente importante para retenção do SaaS.

Se a assinatura custa uma fração disso, a empresa consegue perceber facilmente o retorno sobre o investimento.

---

# 49. Posicionamento

Evitar posicionar o produto simplesmente como:

> Sistema de Ordem de Serviço.

Isso coloca o ServiceSync competindo diretamente com dezenas de sistemas genéricos.

Uma mensagem mais alinhada ao produto seria:

> **ServiceSync organiza seus atendimentos e ajuda sua empresa a trazer seus clientes de volta no momento certo.**

Ou:

> **Faça o serviço hoje. O ServiceSync lembra quando é hora de trazer o cliente de volta.**

A OS é uma funcionalidade.

A recorrência é o diferencial.

---

# 50. Regra para decisões futuras

Toda nova funcionalidade deverá responder pelo menos uma destas perguntas:

1. Ajuda a empresa a executar melhor seus serviços?
2. Ajuda a empresa a organizar seus clientes e equipamentos?
3. Ajuda a empresa a recuperar clientes?
4. Ajuda a transformar uma manutenção prevista em novo atendimento?
5. Reduz trabalho administrativo do prestador?

Caso não responda positivamente a nenhuma delas, provavelmente não pertence ao MVP.

---

# 51. Visão final

O ServiceSync deverá funcionar como uma memória operacional e comercial da empresa.

O profissional não precisa lembrar:

> "Já deve estar na hora de ligar para aquele cliente..."

O sistema sabe.

Ele informa:

> **Você possui 8 clientes que precisam de manutenção esta semana.**

O profissional entra em contato.

O cliente recebe o link.

Escolhe um horário.

A empresa confirma.

Executa o serviço.

Registra uma nova previsão.

E o ciclo recomeça.

Esse deverá ser o princípio central utilizado para orientar produto, arquitetura e desenvolvimento do ServiceSync.
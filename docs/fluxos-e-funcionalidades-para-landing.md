# VoltaMarcada: fluxos e funcionalidades para a landing page

> Documento de referência para copy, estrutura, imagens e futuras melhorias da landing page.
>
> Última atualização: 14 de setembro de 2026.

## O que é a VoltaMarcada

A VoltaMarcada é uma plataforma de gestão para empresas de serviços técnicos e manutenção recorrente. Ela conecta o atendimento de hoje à próxima oportunidade comercial: organiza cliente, equipamento, agenda, orçamento, ordem de serviço e o retorno de manutenção.

O posicionamento central não é apenas “software de ordem de serviço”. O diferencial é ajudar a empresa a não perder clientes já atendidos, indicando quem deve ser contatado para a próxima manutenção e dando meios para transformar esse contato em um novo agendamento.

### Público mais aderente

- Empresas de climatização e ar-condicionado.
- Refrigeração comercial e câmaras frias.
- Assistência técnica, manutenção de equipamentos e instalações.
- Equipes de elétrica, energia solar, piscinas, TI e outros serviços de campo recorrentes.
- Pequenas empresas e profissionais que hoje concentram a rotina em WhatsApp, cadernos ou planilhas.

## Proposta de valor em uma frase

**Organize os serviços de hoje e transforme cada atendimento concluído em uma próxima oportunidade de manutenção.**

## Fluxo principal: ciclo de manutenção recorrente

```text
Cliente e equipamento cadastrados
    -> Ordem de serviço criada e executada
    -> Serviço concluído
    -> Próxima manutenção recomendada
    -> Oportunidade de manutenção aparece para a equipe
    -> Empresa entra em contato e envia um link contextual pelo WhatsApp
    -> Cliente informa dia e horário de preferência
    -> Equipe analisa a agenda, confirma o atendimento e escolhe o profissional
    -> Novo agendamento e/ou nova ordem de serviço
    -> Novo serviço concluído
    -> Novo ciclo de manutenção
```

Esse é o fluxo que melhor traduz o benefício comercial da plataforma: o serviço não termina quando a visita acaba; ele gera uma relação acompanhada e uma chance real de retorno.

## Fluxos que a plataforma suporta

### 1. Entrada e organização inicial da empresa

```text
Cadastro da conta
    -> Criação da empresa
    -> Onboarding com dados da operação
    -> Configuração de identidade, localização, horários e formas de pagamento
    -> Acesso ao painel da empresa
```

A empresa pode manter a própria página pública, marca, dados de contato, localização, fuso/rotina operacional, horários de atendimento e configurações de pagamento. O proprietário também pode convidar colaboradores e administrar o acesso da equipe.

### 2. Atendimento direto: WhatsApp, telefone ou indicação

```text
Cliente entra em contato
    -> Equipe cadastra ou localiza o cliente
    -> Registra endereço e equipamento, quando aplicável
    -> Cria uma ordem de serviço
    -> Agenda uma visita, se necessário
    -> Executa e conclui o serviço
```

O uso do sistema não depende de um agendamento público, orçamento prévio ou aplicativo do cliente. Isso preserva a rotina real de empresas que recebem pedidos por WhatsApp, telefone, indicação ou visita direta.

### 3. Cadastro de clientes, endereços e equipamentos

```text
Cliente
    -> Um ou mais endereços de atendimento
    -> Um ou mais equipamentos
    -> Histórico de serviços, documentos e próximas manutenções
```

Funcionalidades relacionadas:

- Cadastro, edição, busca e exclusão de clientes.
- Múltiplos endereços por cliente.
- Cadastro de equipamentos com dados técnicos e vínculo ao cliente.
- Histórico técnico do equipamento com ordens concluídas, profissional, checklist, fotos, anexos, observações, valor e próxima manutenção relacionada.
- Consulta do contexto antes da visita, sem depender da memória da equipe.

### 4. Serviços e profissionais

```text
Serviços oferecidos
    + Profissionais habilitados
    + Disponibilidade recorrente
    + Bloqueios de agenda
    -> Base para a organização dos atendimentos
```

Funcionalidades relacionadas:

- Catálogo de serviços, duração, preço base opcional e política de preço.
- Serviços que podem ou não aparecer para solicitação online.
- Cadastro de profissionais, inclusive sem acesso de login.
- Associação entre profissional e os serviços que ele pode executar.
- Agenda de disponibilidade por dia e períodos de trabalho.
- Bloqueios de agenda por férias, compromisso, treinamento ou indisponibilidade.
- Solicitação de bloqueio de agenda, com decisão interna quando aplicável.

### 5. Agenda e agendamentos internos

```text
Necessidade de visita
    -> Escolha de serviço, cliente e profissional
    -> Verificação de disponibilidade
    -> Agendamento
    -> Reagendamento ou execução
```

A agenda organiza visitas e responsáveis. Ao criar, aceitar ou reagendar um atendimento, o sistema considera a disponibilidade do profissional, a duração do serviço, bloqueios e os demais compromissos. Uma alteração de horário mantém o histórico do agendamento anterior.

Um agendamento pode representar uma visita técnica, uma execução de serviço ou outro compromisso interno. Ele pode existir com ou sem ordem de serviço, conforme a rotina da empresa.

### 6. Solicitação pública de atendimento

```text
Cliente abre a página pública da empresa
    -> Escolhe um serviço disponível
    -> Informa equipamento e descreve a necessidade, quando solicitado
    -> Escolhe data e horário de preferência
    -> Envia a solicitação
    -> Empresa revisa e confirma ou propõe outro horário
```

A empresa possui uma página pública identificada por seu endereço próprio. O cliente usa o navegador do celular, sem instalar aplicativo e sem visualizar profissionais internos.

O horário escolhido é uma preferência do cliente, não uma confirmação automática. A equipe analisa a disponibilidade real, escolhe o profissional e pode sugerir uma alternativa. Isso evita promessas de agenda que não podem ser cumpridas.

### 7. Orçamentos e aprovação comercial

```text
Solicitação ou necessidade identificada
    -> Orçamento preparado
    -> Link seguro enviado ao cliente
    -> Cliente visualiza, aprova ou recusa
    -> Aprovação cria a ordem de serviço correspondente
```

Funcionalidades relacionadas:

- Orçamentos internos ou originados de uma solicitação.
- Itens de serviço, peças, materiais, deslocamento e itens personalizados.
- Quantidades, descontos, validade, observações e total calculado.
- Link público seguro para consulta, aprovação ou recusa.
- Compartilhamento por WhatsApp com mensagem preparada.
- Geração de PDF do orçamento.
- Criação automática e única da ordem de serviço ao aprovar uma proposta.

### 8. Ordem de serviço e execução em campo

```text
Ordem de serviço criada
    -> Profissional responsável e informações do atendimento
    -> Início da execução
    -> Checklist, fotos, anexos e observações
    -> Itens e valores adicionais, quando necessários
    -> Conclusão ou cancelamento
```

Funcionalidades relacionadas:

- Criação manual de ordem de serviço, mesmo sem orçamento ou agendamento.
- Associação opcional com cliente, equipamento, profissional, orçamento e agendamentos.
- Priorização e acompanhamento por status.
- Checklists de execução e acompanhamento do progresso.
- Fotos de antes e depois do serviço.
- Anexos de imagem ou PDF.
- Registro de itens adicionais, materiais, serviços e descontos.
- Linha do tempo imutável com as ações relevantes na ordem de serviço.
- Histórico técnico acessível a partir do equipamento.

### 9. Próximas manutenções e oportunidades de retorno

```text
Ordem concluída ou oportunidade criada manualmente
    -> Data recomendada para retorno
    -> Painel mostra urgência e contexto
    -> Equipe registra contato
    -> Envia link de agendamento contextual ou inicia orçamento
    -> Oportunidade se converte em novo atendimento
```

Esta é a funcionalidade que diferencia a VoltaMarcada de ferramentas focadas somente em agenda ou ordem de serviço.

A empresa pode criar uma oportunidade de manutenção ao concluir uma ordem ou criá-la manualmente para uma base de clientes já existente. A oportunidade reúne cliente, equipamento, serviço recomendado, data ou intervalo de retorno e observações.

O painel permite priorizar oportunidades vencidas, próximas e futuras, acompanhar contatos e identificar conversões em solicitações, agendamentos, orçamentos, ordens de serviço e receita recuperada.

### 10. Contato e link contextual de manutenção

```text
Oportunidade de manutenção
    -> Preparação de mensagem para WhatsApp
    -> Link seguro e contextual
    -> Cliente escolhe uma preferência de atendimento
    -> Equipe confirma o melhor encaixe
```

O link pode carregar o contexto necessário para o cliente — empresa, serviço recomendado e equipamento — sem expor dados internos. A empresa pode revogar o link quando necessário. O cliente não precisa informar novamente detalhes que a empresa já conhece.

### 11. Painéis de gestão

O painel do proprietário prioriza ações do dia, em vez de apenas números de vaidade:

- Atendimentos de hoje.
- Oportunidades de manutenção vencidas e próximas.
- Solicitações públicas pendentes.
- Orçamentos enviados aguardando decisão.
- Ordens de serviço pendentes ou em execução.
- Indicadores mensais de oportunidades identificadas, clientes contatados, agendamentos recuperados, serviços realizados e receita recuperada por manutenção.

Profissionais e colaboradores têm uma visão operacional mais focada na própria agenda, enquanto a gestão concentra os indicadores de operação e resultado.

### 12. Caixa e pagamentos operacionais

```text
Ordem de serviço concluída com valor
    -> Lançamento pendente no caixa
    -> Proprietário confirma o recebimento e o meio de pagamento
```

O proprietário pode consultar lançamentos por dia, visualizar totais de caixa, criar lançamentos manuais, editar pendências e confirmar recebimentos. Ordens de serviço sem cobrança não criam lançamento financeiro. O produto também permite configurar dados para recebimento por Pix.

### 13. Acesso, segurança e gestão da equipe

- Cadastro, login, recuperação e alteração de senha.
- Perfis de proprietário e colaborador.
- Convites para novos colaboradores e gestão de membros.
- Separação dos dados de cada empresa: uma empresa não acessa os dados de outra.
- Links públicos de orçamento e manutenção protegidos por tokens não previsíveis.
- Licença da empresa, período de teste e renovação por Pix.

## Como os módulos se conectam

```text
Cliente + Equipamento
        -> Solicitação / Atendimento direto
        -> Agenda / Visita técnica
        -> Orçamento (opcional)
        -> Ordem de serviço
        -> Histórico técnico
        -> Próxima manutenção
        -> Contato pelo WhatsApp + link contextual
        -> Nova solicitação ou novo agendamento
```

## Benefícios que a landing pode comunicar com segurança

| Benefício percebido | Funcionalidade que sustenta a mensagem |
| --- | --- |
| Não esquecer clientes que já foram atendidos | Oportunidades de manutenção com data, urgência e acompanhamento |
| Atender com mais contexto | Cliente, equipamentos e histórico técnico conectados |
| Organizar a equipe de campo | Agenda, disponibilidade, bloqueios e responsáveis |
| Profissionalizar o atendimento | Página pública, propostas online e links compartilháveis |
| Fechar serviços com menos fricção | Orçamentos com aprovação pública e geração de ordem de serviço |
| Padronizar a execução | Checklists, fotos antes/depois, anexos e histórico da OS |
| Ter visão da operação | Painel de ações, funil de manutenção e caixa diário |
| Continuar usando WhatsApp como canal comercial | Mensagens preparadas e links seguros para orçamento e retorno |

## Mensagens prioritárias para a landing

1. **O serviço termina. O cliente não.** A ideia de recorrência deve aparecer antes da lista de módulos.
2. **Transforme serviços concluídos em próximos atendimentos.** Explica o valor de negócio de forma direta.
3. **Saiba quem contatar, quando e por qual equipamento.** Traduz a oportunidade de manutenção em benefício prático.
4. **Organize agenda, orçamento e ordem de serviço em um só fluxo.** Mostra que a recorrência não substitui a operação; ela a completa.
5. **Seu cliente solicita pelo celular; sua equipe confirma com segurança.** Resume corretamente a página pública sem prometer confirmação automática.
6. **Atenda com contexto, não com memória.** Conecta cadastro, equipamentos e histórico.
7. **Funciona com a rotina do WhatsApp.** Posiciona o produto junto ao canal que pequenas empresas já usam.

## Pontos que não devem ser prometidos na landing atual

Para manter a comunicação fiel ao produto, evitar promessas de:

- confirmação automática de horário pelo cliente;
- aplicativo mobile nativo para cliente ou equipe;
- pagamento online pelo cliente;
- integração oficial com a API do WhatsApp;
- emissão de nota fiscal, estoque, contabilidade ou folha de pagamento;
- roteirização de equipes;
- CRM ou automações comerciais avançadas;
- escolha de profissional pelo cliente na página pública.

## Sugestão de ordem narrativa para futuras seções da landing

```text
Problema: clientes e retornos se perdem
    -> Diferencial: manutenção recorrente acompanhada
    -> Como funciona: atendimento até o retorno
    -> Recursos operacionais: agenda, orçamento e ordem de serviço
    -> Prova visual: painel de oportunidades e histórico de equipamento
    -> Experiência do cliente: página pública e links por WhatsApp
    -> Resultado: mais organização hoje e mais retornos amanhã
```

## Resumo comercial curto

A VoltaMarcada ajuda empresas de serviços e manutenção a controlar clientes, equipamentos, agenda, orçamentos e ordens de serviço. Seu principal diferencial é acompanhar a próxima manutenção recomendada, mostrar o momento certo de retomar o contato e facilitar a conversão desse retorno em um novo atendimento.

# Entrega — joaopedro1422
---
## 1. Resumo da entrega

Realizei a reestruturação completa do módulo de Beneficiários e da infraestrutura da API 
para atender a todos os requisitos da `SPEC.md`, mantendo a coerência com o padrão do módulo de Planos e aplicando 
boas práticas de arquitetura. Priorizei uma implementação orientada a reutilização de lógicas, legibilidade e facilidade de manutenção.

## API

- **Endpoints** de cobertura ao ciclo de vida do beneficiário (Criação, Recuperação, Listagem paginada com filtros, Atualização cadastral e exclusão lógica), 
  com documentação em `/swagger` e tratamento dos Status Codes previstos (200, 201, 204, 400, 404, 409 e 422).
- **DTOs de entrada e saída:** Contratos estruturais específicos para corpos de criação, atualização, filtragem e listagem (total, pagina, tamanho e dados).
- **Separação de responsabilidades:** Orquestração HTTP via controller, regras de domínio reutilizáveis em `Beneficiario`, regras gerais de negócio + tratamento
  de erros em `BeneficiarioServico` e acesso ao Banco de dados em `AppDbContext`.
- **Regras de negócio e de domínio previstas na SPEC:**
  - Validação de CPF contra sequências de dígitos repetidos, dígitos não numéricos, tamanho (11) e dígitos verificadores válidos.
  - Validação de data de nascimento contra datas futuras.
  - Verificação de plano existente e não excluído logicamente.
  - Bloqueio de alterações cadastrais para usuários INATIVOS.
  - Exclusão lógica com permânencia cadastral do CPF.
  - Tratamento de unicidade para requisições concorrentes com índice único no banco.    
- **Listagem preparada para exibição:** Inclusão do nome do plano na listagem de beneficiários via `Eager Loading (.Include(b => b.Plano))`, realizando um 
  JOIN direto na consulta paginada, entregando os dados prontos para exibição com controle de paginação no cliente sem necessidade de adaptações adicionais.
- **Estruturação de erros e exceções**: resposta clara ao cliente com Mensagem + detalhamento relevante para utilização direta na interface.
- **Logs estruturados:** registro de eventos e pontos críticos em formato estruturado, com informações como ID e resposta HTTP, visando indexação e rastreamento em 
  ferramentas de Observabilidade como Grafana e DataDog.

- **Testes:** Implementação de testes extras para cobrir casos de borda (Data de nascimento futura, plano invalido/excluído, cpf invalido, beneficiario Inativo como 
  registro congelado, cadastro simultâneo de CPFs iguais, bloqueio de novo cadastro para CPF excluído, filtragens , tamanho textual do nome e paginação com parâmetros inválidos), além outros fluxos especificados em `SPEC.md`.

## Banco de Dados

- Migrations: 
  1 - Definição do campo `CPF` como índice único no banco de dados, garantindo unicidade em caso de requisições simultâneas.
  2 - Adição do campo `ExcluidoEm` nullable, para gerenciar a exclusão lógica.

## Frontend

- **Componente de Beneficiários:** Listagem com controle de paginação e quantidade, filtros combináveis para `Status` e `Plano`, cadastro e ações para edição/exclusão.
- **Formulários:** Implementação de modais modernos para cadastro e edição , garantindo melhor usabilidade e evitando navegações desnecessárias entre páginas.
- **Service:** Delegação de contato direto com `HttpCliente` unicamente ao BeneficiarioServico.
- **Validações:** Verificações prévias para regras de domínio (datas de nascimento futuras bloqueadas, CPF obrigatoriamente de tamanho 11, nome entre 3 e 120...).
- Exibição estruturada de erros vindos da API.
- Exibição de feedbacks visuais ao usuário para carregamentos, sucesso de ações, erros e listagem vazia.
- Tratamento para casos de borda na paginação. Ex: página com um único registro > o registro é excluído > volta automaticamente para a página anterior.
- Correção do botão `Recarregar` anteriormente disfuncional no componente de Planos, passando a ser responsável pela atualização da listagem de planos.
---

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

**1. Falha arquitetural na estrutura do BeneficiariosController.**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** Controller com acesso direto ao banco de dados e ausência de service para divisão clara de responsabilidades.
- **Como percebi:** Leitura imediata do código existente.
- **Como corrigi:** Criação do `BeneficiarioServico` para centralizar as regras de negócio e acesso ao banco, além do tratamento e estruturação de erros.
- **O que quebraria em produção:**  A ausência de uma camada de serviço faria o controller acumular responsabilidades indevidas, resultando em acoplamento excessivo, baixa 
  testabilidade e dificuldade de manutenção. 

**2. Corpo de requisições e respostas utilizando entidade de forma inadequada**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** A criação de um novo beneficiário recebia diretamente a entidade `Beneficiario` como corpo da requisição e utilizava a mesma entidade como modelo de 
  resposta.
- **Como percebi:** Leitura imediata do código existente.
- **Como corrigi:** Criação de contratos (DTOs) específicos para cada operação: Criação, atualização, resposta da entidade e envelope de listagem. Respeitando os campos 
  necessários para cada tipo.
- **O que quebraria em produção:**  Utilizar a entidade diretamente como contrato da API criaria forte acoplamento emtre a estrutura interna e a interface externa (cliente),
  qualquer alteração na entidade impactaria o contrato da API, além de expor campos indevidos e desnecessários. Na criação, por exemplo, o contrato `BeneficiarioRequest` espera apenas os campos necessários (Nome, CPF, data de nascimento e Id do plano), deixando a responsabilidade de definição dos campos restantes com a própria aplicação.

**3. Listagem fora da especificação e com N+1**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:**  A listagem não seguia o contrato definido na especificação (Total, página e quantidade) e realizava a busca do Plano individualmente para cada 
  beneficiário, podendo gerar múltiplas consultas ao banco (N+1).
- **Como percebi:** Leitura da `SPEC §3` e análise do código.
- **Como corrigi:** Adequação da resposta ao contrato especificado com `ListaPaginada` (genérica para possível reutilização) e alteração da consulta para carregar os 
  dados necessários de forma conjunta via JOIN com `Eager Loading (.Include(b => b.Plano))`, evitando consultas individuais para cada beneficiário.
- **O que quebraria em produção:** A listagem poderia retornar dados fora do formato esperado pelos consumidores da API. Além disso, conforme a quantidade de 
  beneficiários aumentasse, o número de consultas ao banco poderia crescer proporcionalmente, aumentando a latência e estouros de memória.

**4. Validação incompleta de CPF + Problema de unicidade**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:** A validação verificava apenas o tamanho 11 do cpf, deixando de lado outras regras como dígitos verificadores, sequências repetidas e valores apenas
  numéricos. Além do fato de que o campo `CPF` não era um índice único no banco.
- **Como percebi:** Leitura das definições de domínio `SPEC §2.3` e análise do código
- **Como corrigi:** Criação de função reutilizável para validar CPFs, verificando todas as regras previstas e migration para definir Cpf como `Unique`.
- **O que quebraria em produção:** O usuário poderia incluir letras, informar sequências como `11111111111` ou com códigos inválidos. Além de que, duas requisições
  simultâneas poderiam quebrar a exigência para CPFs únicos da `SPEC.md`.

**5. Status codes incorretos**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Controllers/BeneficiariosController.cs`
- **O que estava errado:**  A criação de beneficiários respondia 200 OK, enquanto a especificação era para 201 + `location`. Além disso, o erro para CPF duplicado estava 
  como 400 BadRequest, enquanto o esperado é 409.
- **Como percebi:** Leitura da `SPEC §.3` e testes vermelhos.
- **Como corrigi:** Controller com respostas bem definidas e condizentes com cada erro. `ConflitoException` e `CreatedAtAction`.
- **O que quebraria em produção:** Retornos incorretos para clientes, resultando em quebras de fluxo e inconsistências.

**6. Encapsulamento inadequado na entidade Benficiario + ausência de campo `ExcluidoEm` para exclusão lógica**

- **Onde:** `base/backend-dotnet/src/Desafio.Api/Dominio/Beneficiario.cs`
- **O que estava errado:**  Os setters dos atributos da entidade eram públicos, permitindo que qualquer parte da aplicação alterasse diretamente seu estado sem passar
  pelas regras de negócio. Além disso não existia campo para gerenciar a exclusão lógica
- **Como percebi:** Leitura imediata do código
- **Como corrigi:** Restrição dos setters para private e centralização das alterações por meio do método `DefinirDados` garantindo que mudanças de estado passem 
  sempre pelas regras definidas. Tmbém adicionei o campo `ExcluidoEm`.
- **O que quebraria em produção:** possibilidade de dados invalidos, cpfs não verificados, datas de nascimento futuras, nomes vazios e alteração de ID.

### 2.2 Pontos em que a especificação não definiu o comportamento

**1. Ordenação da listagem de beneficiários**

- **O que a spec não define:** A ordem dos registros retornados.
- **O que decidi:** Ordenar pela data de cadastro (DESC), do mais recente para o mais antigo + Desempate por ID (ASC).
- **Por quê:** Priorizar os beneficiários mais recentes na exibição da interface e garantir estabilidade para a listagem.
- **O que eu consideraria se fosse decidir diferente:** Caso existise uma necessidade de negócio específica, ordenação também por nome em ordem alfabética.

**2. Forma de obter o nome do plano**

- **O que a spec não define:** Se o nome do plano deveria ser obtido e retornado pela api ou resolvido pelo frontend utilizando o `planoId`.
- **O que decidi:** Retornar o nome do plano diretamente pelo backend, junto aos dados do beneficiário.
- **Por quê:** Completude de informações necessárias para exibição da listagem, tornando a funcionalidade reutilizavel em outros módulos do frontend em que não
  houvessem a listagem de planos desde o início. Evita adaptações desnecessárias ao cliente.
- **O que eu consideraria se fosse decidir diferente:** O frontend poderia utilizar o PlanoId para realizar a associação com a lista de planos já carregada. Porém
  tornaria a funcionalidade sempre dependente de chamadas ao serviço de planos em outros modulos do sistema que necessitassem listar os beneficiários.

**3. Regra de alteração de status e edição de beneficiários inativos**

- **O que a spec não define:** A alteração entre os status Ativo e Inativo deve ocorrer sempre por meio da atualização completa (PUT). Além disso, beneficiários 
  inativos não podem ter seus demais dados alterados. As duas regras, quando aplicadas simultaneamente, criavam uma inconsistência: um beneficiário inativo não poderia ter seus dados alterados, mas também não haveria outra operação para alterar seu status de volta para Ativo.
- **O que decidi:** Permitir a atualização quando o beneficiário estiver Inativo e o Status enviado no PUT for Ativo. Caso ambos sejam Inativo, a alteração é bloqueada.
- **Por quê:** Dessa forma, a regra de que beneficiários inativos não podem ter seus dados alterados é preservada, ao mesmo tempo em que o próprio PUT permite reativá-los.
- **O que eu consideraria se fosse decidir diferente:** Criaria uma operação específica para alteração de status, caso a regra de negócio permitisse separar 
  essa responsabilidade da atualização completa.

**4. Ordem de validação na atualização de beneficiários**

- **O que a spec não define:** ordem em que as regras de validação deveriam ser executadas durante a atualização. Por exemplo: corpo de atualização vindo com Status inválido
  , informações cadastrais inválidas (nome, cpf, data de nascimento) e plano inválido, qual deve ser a resposta?
- **O que decidi:** Validar primeiro a existência e validade do status (estritamente 'Ativo' ou 'inativo'), seguida pela validação para bloqueio de alteração das informações
  caso o corpo (PUT) envie status `Inativo`, após isso verifica a existência/não exclusão do plano informado e posteriormente valida dados cadastrais de domínio em 
  `Beneciario.cs`.
- **Por quê:** O status é deterministico (Ativo ou Inativo) e é fundamental para a verificação posterior de congelamento de atualização. Após isso, o vínculo com plano
  é obrigatório, caso o plano seja inválido a aplicação não perde tempo verificando os demais dados cadastrais e prioriza mensagem indicando erro no `PlanoId`. Por fim, com
  todos os fluxos verificados, valida os dados cadastrais no domínio.
- **O que eu consideraria se fosse decidir diferente:** Poderia verificar primeiro o Plano ID informado. De resto, o fluxo precisa seguir o atual para cobertura completa.


### 2.3 Inconsistências que percebi

**1. Valor default de tamanho da listagem (10 vs 20)**

- **A spec diz:** Caso não seja especificado, o tamanho deve ser igual a 10
- **O teste espera:** `Listar_sem_informar_tamanho_deve_devolver_20_itens_por_pagina` espera 20 itens.
- **Segui:** A `SPEC.md`, alterando o teste para esperar e resposta correta (10)
- **Por quê:** A prioridade deve ser o contrato de requisitos do cliente para referenciar a implementação. Em projetos reais, caberia comunicação com a 
  equipe para identificar se o teste está desatualizado ou foi implementado propositalmente por inconsistência da SPEC. Neste caso, a SPEC foi clara.

**2. Atualização cadastral de beneficiário `Inativo` (409 vs 200)**

- **A spec diz:** Caso o beneficiário esteja Inativo, a alteração de dados deve ser bloqueada, respondendo 409.
- **O teste espera:** Sucesso (200OK) ao alterar dados de beneficiário inativo.
- **Segui:** A `SPEC.md`, alterando o teste para esperar a resposta correta (409)
- **Por quê:** A Spec foi clara quanto ao congelamento de beneficiários inativos. Para manter essa regra e ainda permitir a reativação, a implementação bloqueia a 
  atualização quando o beneficiário está `Inativo` e o status enviado também é `Inativo`. Caso o status enviado seja `Ativo`, a reativação é permitida junto à atualização
  dos demais dados.

**3. Problema conceitual na especificação sobre exclusão lógica de planos**

- **A spec diz:** que um plano excluído logicamente não pode ser referenciado por novos beneficiários nem por atualizações, e que beneficiários que já apontavam para
  o plano no momento da exclusão continuam válidos e vinculados a ele.
- **Problema:** Se um beneficiário é vinculado ao plano X, e o plano X é excluído, o beneficiário não conseguiria receber nenhuma atualização de dados cadastrais mantendo 
  o plano atual, já que este não passaria na verificação de plano presente na atualização de beneficiário (que protege contra planos inexistentes e excluídos). Isso 
  implicaria em uma mudança de Plano forçada ao precisar realizar alterações cadastrais no beneficiário. 
- **Decidi:** Apenas aplicar a funçao de verificação de plano existente/não-excluído no `PUT /Beneficiarios` caso o PlanoId recebido no novo corpo seja diferente do PlanoId 
  atual do beneficiário.
- **Resultado:** Caso este cenário aconteça, os dados cadastrais do beneficiário poderão ser alterados mesmo que o seu plano atual esteja excluído. 
  Evitando uma alteração forçada de vínculo com o Plano.


### 2.4 Decisões técnicas
#### API
- **Domínio responsável pela verificação de entrada de dados:** Coloquei todos os setters como privados para garantir encapsulamento e tornei o método `DefinirDados` como
  a única forma de alterar os dados cadastrais na entidade. Implica em reutilização em outras partes da API de forma segura e consistente.
- **Separação de responsabilidades no serviço:** Divisão das regras em funções privadas, nomeadas de forma clara de acordo com suas responsabilidades, facilitando leitura,
  manutenção e evolução reutilizável do código conforme crescimento de funcionalidades.
- **DTOs de entrada e saída centralizados em `\Contratos\BeneficiarioContratos.cs`**, com estruturas definidas para cada tipo de operação (Criação, atualização, resposta). DTO de paginação `ListaPaginada<T>` genérica para reutilização em outras partes do sistema (listagem de unidades, funcionarios e etc.).
- **Nome do plano na listagem dos beneficiários:** Resposta de beneficiários contendo o nome do plano em sua estrutura, obtido por meio do relacionamento com a tabela de 
  Planos na própria consulta utilizando um JOIN e evitando o problema do N+1.
- Registro de logs estruturados com `ILogger` para mapeamento de pilha.
- **Sequência lógica de verificações na Atualização de beneficiário:** Status válido (Ativo ou Inativo) -> Tentativa de alteração de dados cadastrais com status inativo -> 
  Verificação de existência/não exclusão do plano informado -> Validação de dados no domínio `Beneficiario.cs`.
- **Padronização do parseamento JSON em `snake_case`:** Configuração global do `PropertyNamingPolicy` em `Program.cs` para garantir que requisições e respostas considerem 
  por padrão o snake case para parseamento. Por exemplo: `POST | exemplo_disso -> exemploDisso ` , `GET | exemploDisso -> exemplo_disso`. (Auxílio da IA para obter a função exata que configura este comportamento globalmente).
- Utilização do `Postman` para testes manuais além dos testes implementados em `BeneficiarioTestes.cs`;

#### Frontend
- Organização de pastas e arquivos auxiliares de acordo com o componente aos quais pertencem.
- Tratamento para casos de borda na paginação. Ex: página com um único registro > o registro é excluído > volta automaticamente para a página anterior. 
- Bloqueio de botões Salvar/Atualizar em caso de regras cadastrais não atendidas (Nome 3-120, data de nascimento futura, CPF limitado a 11, apenas numérico).
- Comunicação com a API centralizada exclusivamente no `BeneficiarioServico.ts`.
- Utilização da biblioteca `FontAwesome` para ícones de ações e Spinner indicador de carregamento.
- Controle de paginação com `Angular Material Paginator`, componente padrão que simplifica a implementação e mantém o HTML mais enxuto.

- **Cache compartilhado** dos planos no `PlanoServico`, utilizando `shareReplay(1)` para otimizar o consumo da API `GET/planos` e dispensar requisições repetitivas pelo módulo de Beneficiários e futuros módulos 
  do sistema em caso de projeto real. A primeira chamada à API `GET/planos` armazena o resultado em cache em memória no PlanoServico. Chamadas subsequentes (como filtros, seleção nos modais de cadastro e edição) reutilizam esse cache instantaneamente.
  - **Invalidação via `refresh = true`:** O botão `Recarregar` já existente é responsável por atualizar o cache e sempre busca os planos na API (através do `refresh = true`).
    garantindo a atualização do cache sob demanda após mutações nos planos (CRUD).
  - **Decisão e contexto:** A decisão considera que a lista de planos é pequena e raramente atualizável, sendo utilizada como dado de referência por outras funcionalidades 
    e páginas futuras do sistema (filtros, cadastro, edição etc.). A implementação foi feita unicamente para demonstrar a minha capacidade de decisões arquiteturais orientadas ao contexto e experiência prévia com cache, reconhecendo que, para a escala atual, não  há ganho de performance e os planos poderiam ser obtidos por outras chamadas à API diretamente sem maiores problemas. 
    Decidi este caminho pela baixa complexidade de implementação, poucas linhas alteradas apenas no `PlanoServico`. Todo o resto da aplicação chama o serviço normalmente.

### 2.5 O que ficou de fora

Nada. A implementação atende a todos os requisitos da `SPEC.md`; o resultado da suíte pública é verde e o `verificar.sh` roda corretamente.
---

## 3. Uso de IA


**Nível de uso:** Moderado

### 3.1 Ferramentas

*Microsoft Copilot (Desktop):* 
- Aceleração do desenvolvimento com funções auxiliares específicas.
- Explicação para compreensão de partes do código antigo. 
- Apoio na revisão final do código com base na `SPEC.md`.
- Apoio na implementação da verificação de dígitos verificadores válidos de CPF.
- Revisão da cobertura de testes para os casos de borda e sugestão de novos casos pertinentes.
- Sugestão da utilização de `Task.WhenAll` para executar requisições simultâneas no teste de concorrência do cadastro de CPF.
- Resolução de problemas com o docker na minha máquina durante o desenvolvimento.
- Estilização primária de alguns componentes do frontend com minha orientação. Ajustes finos feitos por mim.

### 3.2 Os 3 prompts que mais influenciaram o resultado

**Prompt 1**

```
Com base nesta especificação de requisitos, analise a minha cobertura de testes e sinalize possíveis casos que estão faltando.
```

- **O que aceitei:** Testes para atualização passando status invalidos (fora do enum Ativo/Invativo) e CPF alterado (deve ignorar), para verificação da quantidade
  de erros estruturados após tentar criar um beneficiário com varios campos inválidos, para tentativa de criação com CPF com digitos verificadores inválidos e entre outros 
  casos que considerei relevantes para o contexto.
- **O que descartei e por quê:** Casos que não se aplicavam a spec ou já eram contemplados em outros testes.


**Prompt 2**
```
Como posso implementar um teste que realize múltiplas requisições simultâneas para verificar a unicidade de cadastros em caso de concorrência (.NET / C#) ?
```

- **O que aceitei:** Utilização de Task.WhenAll para executar requisições simultaneamente e verificar se, diante de múltiplas tentativas de cadastro com o mesmo CPF, 
  apenas uma é concluída com sucesso e as demais são rejeitadas.
- **O que descartei e por quê:** Abordagens mais complexas para simular concorrência que não eram necessárias para o escopo do teste, mantendo a implementação baseada
  nos recursos nativos do .NET..

**Prompt 3**
```
Para este formulario html, estilize de forma moderna, com direção vertical dos campos, espaçamento adequado entre label/campo e campo/campo, botões alinhados à 
direita para cancelar e Salvar, mensagem de erro em vermelho,background dos inputs levemente mais claros do que o modal e estilização de :focus.
```
+
```
Seguindo o mesmo padrão do formulario de cadastro, estilize este modal de edição, mantendo CPF e data de nascimento alinhados no topo dentro de um card em pequeno destaque
e o restante seguindo a mesma estilização.
```

- **O que aceitei:** A estrutura inicial de estilização em CSS, utilizando-a como base e realizando posteriormente ajustes manuais de cores, espaçamentos, 
  alinhamentos e demais detalhes visuais.
- **O que descartei e por quê:** Sugestões com cores ou animações exageradas, priorizando uma interface mais limpa e consistente com o contexto de um sistema corporativo.


### 3.3 O que fiz sem IA 
### API

- Identificação e correção imediata de problemas arquiteturais ao analisar o código antigo: Criação 
  do BeneficiarioServico, criação de contratos (DTOs) de entrada e saída para beneficiários, problemas de encapsulamento da entidade `Beneficiario.cs` e listagem de baixa performance com N+1.
- Leitura atenta à `SPEC.md` para entender o contexto e regras especificadas.
- Estruturação correta do Controller: definição de contratos explícitos de resposta para cada endpoint, contendo Status code e objeto de Exceção esperados.
- Método `DefinirDados` no domínio `Beneficiario.cs` verificando todas as regras de domínio das informações cadastrais ( Validação de CPF por IA ) e estruturando 
  resposta de erros com base no padrão da casa: Mensagem de erro + lista de campos com identificação e tipo de erro (obrigatorio, invalido, data futura e etc.). 
- Implementação do CRUD no service (criação, recuperação por ID, listagem, atualização e exclusão), atendendo as especificações com foco na separação de responsabilidades
  em funções auxiliares de fácil interpretação. Mantendo as funções principais enxutas para melhor legibilidade.
- Tomadas de decisões sobre fluxos de atualização, ordem de erros e forma de retornar o nome do plano na listagem de beneficiários.
- Tomada de decisão sobre seguir a `SPEC.md` contra os dois testes incorretos.
- Registro de Logs estruturados com `ILogger`.
- Implementação de alguns testes na suite + testes rápidos com Postman.

### Frontend
- Funções de acesso ao HttpClient no serviço,.
- Lógicas typescript no componente de Beneficiários para chamada do serviço, tratamento de erros e verificações prévias de informações inválidas. 
- Utilização do componente de paginação nativo do Angular Material.
- Criação / Chamada dos modais de criação e edição.
- Campos de filtragem por Planos e status + botão de busca.
- Estrutura de cache em memória para listagem de Planos, utilizando `ShareReplay` para armazenar e flag booleana `refresh` para limpeza do cache e nova busca em `GET/Planos`
- HTML em geral.
- Sobre IA: Utilizo como aceleradora do desenvolvimento seguindo as decisões e direcionamentos definidos por mim. Ela foi utilizada como ferramenta auxiliar 
  para processos repetitivos, funções auxiliares de apoio ao que estou implementando, revisão de código e esclarecimento de dúvidas técnicas. Decisões arquiteturais e 
  implementação das principais regras de negócio permanecem sob minha responsabilidade.

### 3.4 O que ainda não domino

Tenho domínio sobre as decisões e implementações realizadas por mim no desafio - experiência prévia com .NET e Angular em projetos corporativos.
Não domino em profundidade o funcionamento interno do EF Core relacionado às Migrations, mas consigo utilizar e tenho noção de que ele detecta mudanças em 
relação ao estado anterior e gera a nova migration com as operações necessárias para refletir essas mudanças no banco de dados.

---

## 4. Perguntas de compreensão

### 4.1 Concorrência

Em caso de duas ou mais requisições concorrentes que busquem criar um Beneficiário com o mesmo CPF, apenas uma delas será aceita a nível de banco de dados, retornando 201 OK para esta e `ConflitoException 409` para as demais, em hipótese alguma existirão dois CPFs iguais na tabela. Isso é possível pela definição do campo CPF como índice único em `Infraestrutura/AppDbContext.cs` através de 
`entidade.HasIndex(b => b.Cpf).IsUnique();` e nova migration `AdicionaIndiceUnicoCpf`, que permite ao banco garantir a unicidade de forma nativa. Ao receber uma violação do índice único, o Postgresql lança uma exceção de código `23505`, que será verificado e capturado em `SalvarAsync()` para retornar o erro esperado pela spec (409). Há uma checagem prévia `BeneficiarioServico.VerificaCpfExistente`, que invalida casos de mal uso (tentativa de cadastrar CPF previamente já cadastrado), mas que não protege contra concorrência. Esta validação prévia também garante que CPFs de beneficiários excluídos logicamente continuem indisponíveis para novos cadastros.

### 4.2 Um defeito que você corrigiu

No código original, o método de listagem em `BeneficiariosController.cs` buscava todos os beneficiários do banco de dados sem paginação e filtragem, aplicando uma requisição a Planos para cada beneficiario retornado, resultando em complexidade N+1, que pode gerar diversos problemas em produção como: estouro de memória, queda da API, Timeouts e etc. Além de uma busca extremamente lenta. 

*foreach (var b in lista)
  {
      b.Plano = await _db.Planos.FindAsync(b.PlanoId);
  }*   

Para uma listagem de 100 beneficiários, a aplicação executava 1 consulta inicial + 100 consultas adicionais ao banco de planos.
Corrigi esse defeito alterando a consulta para aplicar Eager Loading com `.Include(b => b.Plano)`. Dessa forma, a aplicação executa apenas uma única query SQL com JOIN no Postgresql e retorna os dados completos (com Plano vinculado para obter o nome) de forma nativa e segura.
Também implementei o retorno correto da listagem, envelopado com Total (`var total = await query.CountAsync(cancellationToken);` - query leve apenas para obter o total de registros que satisfazem os fitros), pagina (`.Skip((pagina - 1) * tamanho)`) e tamanho (`.Take(tamanho)`). Além dos filtros combináveis com ` query = query.Where(b => b.Status == status.Value)` e `query = query.Where(b => b.PlanoId == planoId.Value)`. Dessa forma, a listagem acontece de forma performática e completa em seus requisitos.


### 4.3 O trecho mais complexo

Na minha opinião, o trecho mais complexo é a função `AtualizaBeneficiario()` em `BeneficiarioServico.cs`, não por complexidade de código, mas pela combinação de múltiplas regras de negócio e tratamentos de erros, que se não implementados da forma e ordem corretas, podem gerar inconsistências. 
Por exemplo: se primeiro eu verificar se o status é `Inativo` para bloquear ou não a alteração dos outros dados, mas não verificar previamente se o tipo de status vindo na requisição é valido para o Enum Ativo ou Inativo, uma requisição com status = `Congelado` sofrerá com exceções não tratadas pela aplicação.  Assim como, se um beneficiário estava Inativo mas recebe corpo de atualização com status Ativo, deve permitir a alteração dos demais dados cadastrais, caso o corpo continue trazendo Inativo, deve bloquear.
Resolvi todas essas regras e tratamentos de erros com uma função principal enxuta, delegando verificações mais extensas a funções auxiliares reutilizáveis com nominações que descrevem bem a sua responsabilidade: 
```csharp
public async Task<Beneficiario> AtualizaBeneficiario(Guid id, BeneficiarioAtualizacaoRequest dados, CancellationToken cancellationToken)
{
    logger.LogInformation("Iniciando atualizacao do beneficiario {BeneficiarioId}", id);
    var beneficiario = await ObterPorIdAsync(id, cancellationToken);

    if (!Enum.TryParse<StatusBeneficiario>(dados.Status, ignoreCase: true, out var novoStatus))
    {
        throw new ValidacaoException("O Status informado é invalido");
    }
    // Função com 8 linhas e nominação clara da sua responsabilidade (validar se ha tentativa de alteraçao de dados para beneficiario inativo e lançar erro adequado)
    ValidarAlteracaoDeBeneficiarioInativo(beneficiario, novoStatus, dados);

    if (dados.PlanoId.HasValue && dados.PlanoId.Value != beneficiario.PlanoId)
    {
      // Função com 6 linhas para verificar se o plano existe e/ou está excluído logicamente e lançar erro adequado
      await VerificaPlanoExistente(dados.PlanoId.Value, cancellationToken);
    }

    beneficiario.DefinirDados(dados.NomeCompleto!.Trim(), beneficiario.Cpf, dados.DataNascimento, dados.PlanoId, dados.Status);
      
    await SalvarAsync(cancellationToken);
    logger.LogInformation("Beneficiario {BeneficiarioId} atualizado com sucesso", id);
    return beneficiario;
}
```
A função principal, que deveria ter mais de 26 linhas de código escrito (sem contar validações de domínio) para atender aos requisitos, possui 12 linhas objetivas e cobre  corretamente todos os cenários de erro, além de permitir que outros métodos reusem as verificações de plano e congelamento de registros.

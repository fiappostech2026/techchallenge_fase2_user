# FCG.Usuario — Microsserviço de Usuários

Este repositório contém o **microsserviço de Usuários** do sistema FIAP Cloud Games (FCG).
Ele é uma das quatro peças independentes que formam o sistema — para entender como ele se
conecta com as outras, veja o repositório
[`FCG.Orchestration`](../FCG.Orchestration),
que explica o projeto como um todo em linguagem simples.

Este README explica só a parte deste serviço, também em linguagem simples, assumindo que quem
lê não tem experiência técnica.

---

## 1. O que este serviço faz

Este serviço é o **provedor de identidade** da plataforma: é ele quem cuida do **cadastro** de
novos usuários e do **login**. Quando alguém faz login com sucesso, este serviço emite um
"crachá digital" (um token **JWT**) que os outros serviços (como o Catálogo) usam para saber
quem é o usuário e se ele tem permissão para fazer cada coisa.

Diferente dos serviços de Pagamentos e Notificações, este **tem uma API HTTP** — ou seja, outros
sistemas (como um site ou aplicativo) se conectam diretamente nele. Ele também guarda seus
próprios dados em um banco de dados (SQL Server), assim como o Catálogo.

Ele oferece três grupos de funcionalidades:

1. **Cadastro de usuário** (`POST /api/Usuario/criar`) — cria uma nova conta com nome, e-mail e
   senha. A senha nunca é guardada como texto puro: ela é convertida em um "resumo embaralhado"
   (hash **BCrypt**) antes de ir para o banco.
2. **Login / autenticação** (`POST /api/Usuario/login`) — confere e-mail e senha e, se estiverem
   corretos, devolve um token JWT.
3. **Health check** (`GET /api/health`) — um endereço simples que responde "estou no ar", usado
   para monitoramento. Há também um `GET /api/health/secure`, que só responde se você mandar um
   token válido (serve para testar se a autenticação está funcionando).

### O fluxo de cadastro, passo a passo

1. Alguém pede para criar uma conta (`POST /api/Usuario/criar`).
2. Este serviço confere se o e-mail já existe. Se já existir, responde `400` com a mensagem
   `"Usuário já cadastrado."`.
3. Se for um e-mail novo, ele salva o usuário (com a senha em hash BCrypt) e publica um aviso,
   `UserCreatedEvent`, contando o ID e o e-mail do novo usuário.
4. O microsserviço de Notificações **recebe** esse aviso e "envia" (simula, via log) um e-mail de
   boas-vindas.

```
POST /api/Usuario/criar  ──►  UserCreatedEvent  ──►  NotificationsAPI recebe  ──►  "envia" e-mail de boas-vindas
```

### O que acontece quando o login falha

Se o e-mail não existir **ou** a senha estiver errada, o login responde `401 Unauthorized` com a
mensagem genérica `"E-mail ou senha inválidos."`.

A mensagem é **propositalmente genérica**: ela não diz se foi o e-mail que não existe ou a senha
que está errada. Isso é uma escolha de segurança — se a resposta fosse diferente para cada caso,
alguém mal-intencionado poderia usar isso para descobrir quais e-mails têm conta na plataforma
(um ataque chamado *enumeração de usuários*).

---

## 2. Como as peças se conectam (RabbitMQ e HTTP)

Este serviço se conecta ao resto do sistema de duas formas:

- **HTTP**, para receber pedidos diretos (cadastro, login, health check).
- **RabbitMQ**, um "correio" compartilhado entre todos os microsserviços, para avisar quando um
  novo usuário é criado (veja a explicação completa no README do repositório de orquestração).

| Direção | Evento | Exchange no RabbitMQ | Fila (queue) que recebe |
|---|---|---|---|
| Envia (publica) | `UserCreatedEvent` | `user-created-event` | `notifications-user-created-event` (do lado do Notifications) |

Este serviço **apenas publica** eventos — hoje ele não consome nenhum evento de outro serviço,
então não tem fila própria de recebimento.

A biblioteca usada para falar com o RabbitMQ é o **MassTransit**, um "tradutor" que evita ter
que lidar com os detalhes técnicos do protocolo do RabbitMQ na mão. A serialização das mensagens
usa `UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true)` em vez do envelope padrão do
MassTransit — porque cada serviço define sua própria cópia local de cada evento, em seu próprio
namespace, então a correspondência das mensagens é feita pela **estrutura do JSON**, não pelo nome
técnico da classe.

> **Por que a fila tem o prefixo `notifications-`?** Cada serviço prefixa o nome das suas filas
> com o próprio nome (via `SetEndpointNameFormatter`). Isso evita que dois serviços independentes
> que consomem o mesmo tipo de evento acabem compartilhando uma única fila física — se isso
> acontecesse, as mensagens seriam divididas entre eles em vez de cada serviço receber todas.

---

## 3. Estrutura de pastas

```
techchallenge_fase2_user/
├── FCG.Usuario.Domain/           # regras de negócio (não sabe nada sobre banco/RabbitMQ/Web)
│   ├── Configurations/            # JwtSettings — configurações lidas do appsettings
│   ├── Dto/                       # formato do evento publicado (UserCreatedEvent)
│   ├── DTOs/                      # formato das requisições/respostas (CriarUsuarioDto, LoginDto, LoginResponseDto)
│   ├── Entities/                  # UsuarioEntity — o que é salvo no banco
│   ├── Enums/                     # PerfilEnum (Usuario, Admin)
│   ├── Interfaces/                # contratos dos serviços e do repositório
│   ├── Services/                  # UsuarioService (cadastro/login) e JwtService (gera o token)
│   └── Validators/                # validação das requisições (FluentValidation)
├── FCG.Usuario.Infra/             # acesso a dados (Entity Framework Core + SQL Server)
│   ├── Context/                   # UsuarioDbContext — a conexão com o banco
│   ├── Mappings/                  # mapeamento das entidades para tabelas
│   ├── Migrations/                # histórico de alterações no banco
│   └── Repositories/              # UsuarioRepository — implementação do acesso a dados
├── FCG.Usuario.WebApi/            # ponto de entrada do programa (API HTTP)
│   ├── Controllers/               # UsuarioController (criar/login), HealthController
│   ├── Extensions/                # configuração (banco, JWT, RabbitMQ, injeção de dependência)
│   ├── Properties/                # perfis de execução (launchSettings)
│   └── Program.cs                 # arquivo que liga tudo e inicia o serviço
├── FCG.Usuario.Tests/             # testes unitários automatizados (xUnit + Moq) — ver seção 7
│   ├── Services/                  # testes do UsuarioService (cadastro/login) e do JwtService (token)
│   └── Validators/                # testes da validação do cadastro (CriarUsuarioValidator)
├── k8s/                            # manifestos de deploy no Kubernetes (ver seção 6)
│   └── secret.yaml.example        # MODELO do Secret, sem valores reais (o secret.yaml de verdade nunca vai para o Git)
├── azure-pipelines.yml            # pipeline de CI/CD no Azure DevOps (ver seção 8)
├── Dockerfile                      # receita para empacotar este serviço em um container
└── .dockerignore                   # lista do que NÃO entra na imagem Docker (bin/, obj/, k8s/, etc.)
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ e um SQL Server disponíveis —
útil para testar sem precisar subir o projeto inteiro.

> **Importante:** nenhuma senha ou chave fica gravada no código nem no `appsettings.json`. Você
> precisa informar esses valores na hora de rodar (veja abaixo). Se a chave do JWT
> (`Jwt:SecretKey`) não for informada, o serviço **se recusa a iniciar** e mostra o erro
> `Jwt:SecretKey não configurada.` — isso é proposital, para nunca subir sem segurança.

### Opção A — via .NET direto (para quem tem o SDK instalado)

Os segredos ficam guardados no **`dotnet user-secrets`**, um "cofre" do próprio .NET que fica na
sua máquina, fora da pasta do projeto (então não corre o risco de ir para o Git). Basta fazer isso
uma vez:

```bash
dotnet user-secrets set "Jwt:SecretKey" "<mesma-chave-jwt-do-catalogo>" --project FCG.Usuario.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=users;User Id=sa;Password=<sua-senha>;TrustServerCertificate=True" --project FCG.Usuario.WebApi
dotnet user-secrets set "RabbitMQ:Username" "guest" --project FCG.Usuario.WebApi
dotnet user-secrets set "RabbitMQ:Password" "guest" --project FCG.Usuario.WebApi
```

Depois é só rodar:

```bash
dotnet run --project FCG.Usuario.WebApi
```

> O `user-secrets` só é lido no ambiente de desenvolvimento (`Development`), que é o padrão do
> `dotnet run` neste projeto. Como alternativa, você também pode passar os mesmos valores como
> **variáveis de ambiente** (por exemplo `Jwt__SecretKey` e `ConnectionStrings__DefaultConnection`
> — ver seção 5).

### Opção B — via Docker

Aqui os segredos são passados como variáveis de ambiente (`-e`) na hora de iniciar o container:

```bash
docker build -t fcg-users-api:latest .
docker run --rm \
  -e RABBITMQ__HOST=host.docker.internal \
  -e RABBITMQ__USERNAME=guest \
  -e RABBITMQ__PASSWORD=guest \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=users;User Id=sa;Password=<sua-senha>;TrustServerCertificate=True" \
  -e Jwt__SecretKey="<mesma-chave-jwt-do-catalogo>" \
  -p 5001:8080 \
  fcg-users-api:latest
```

> O container roda com um **usuário comum, sem privilégios de administrador (root)** — uma boa
> prática de segurança: se alguém conseguisse invadir o container, teria poderes limitados.

> Para rodar o sistema completo (com RabbitMQ, o banco de dados e o serviço de Notificações
> juntos, prontos para o fluxo de cadastro funcionar de ponta a ponta), use o `docker-compose.yml`
> do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

A coluna **"Segredo?"** indica se o valor é sensível. Valores sensíveis **nunca** ficam no Git:
localmente vêm do `dotnet user-secrets` ou de variáveis de ambiente; no Kubernetes (AKS) vêm do
Secret `users-secret`, que a pipeline cria a partir dos *Variable Groups* do Azure DevOps (ver
seção 8).

| Variável | Descrição | Segredo? | Valor padrão (local) | De onde vem no AKS |
|---|---|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | Sim (endereço externo) | `localhost` | Secret ← `rabbitmq-host` (`fcg-shared-secrets`) |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | Sim | `/` | Secret ← `rabbitmq-vhost` (`fcg-shared-secrets`) |
| `RabbitMQ__Port` | Porta do RabbitMQ (opcional) | Sim | — (porta padrão) | Secret ← `rabbitmq-port` (`fcg-shared-secrets`) |
| `RabbitMQ__UseSsl` | Se a conexão com o RabbitMQ usa criptografia (TLS) | Sim | `false` | Secret ← `rabbitmq-use-ssl` (`fcg-shared-secrets`) |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | **Sim** | `guest` (se nada for informado) | Secret ← `rabbitmq-user` (`fcg-shared-secrets`) |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | **Sim** | `guest` (se nada for informado) | Secret ← `rabbitmq-password` (`fcg-shared-secrets`) |
| `ConnectionStrings__DefaultConnection` | Endereço e credenciais do banco de dados SQL Server | **Sim** | — (obrigatório informar) | Secret, montado pela pipeline com `users-sql-password` (`fcg-users-secrets`) |
| `MSSQL_SA_PASSWORD` | Senha do administrador do SQL Server (usada pelo container do banco) | **Sim** | — | Secret ← `users-sql-password` (`fcg-users-secrets`) |
| `Jwt__SecretKey` | Chave secreta usada para assinar o "crachá digital" (JWT) do usuário | **Sim** | — (obrigatório; sem ela o serviço não inicia) | Secret ← `jwt-secret-key` (`fcg-shared-secrets`) |
| `Jwt__Issuer` | Quem emite o token (carimbado no JWT) | Não | `FCG.Catalog.Api` (k8s/local) — `FCG.Usuario.Api` (docker-compose) | ConfigMap `users-config` |
| `Jwt__Audience` | Para quem o token se destina (carimbado no JWT) | Não | `FCG.Catalog.Client` (k8s/local) — `FCG.Usuario.Client` (docker-compose) | ConfigMap `users-config` |

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

> **Por que o endereço do RabbitMQ também é tratado como segredo?** Porque em produção ele aponta
> para um serviço externo (por exemplo, um RabbitMQ na nuvem), e o endereço junto com o usuário
> já dá uma pista de onde atacar. Por isso todas as configurações do RabbitMQ ficam juntas no
> Secret, e o ConfigMap guarda só o que é realmente público.

> **Acoplamento importante com o Catálogo:** o Catálogo valida os tokens JWT emitidos por este
> serviço, e para isso os dois precisam usar **exatamente a mesma** `Jwt__SecretKey` /
> `JwtSettings__SecretKey`, além do mesmo `Issuer`/`Audience`. Isso já é garantido hoje em cada
> ambiente (corrigido depois de um 401 causado por divergência entre os dois serviços):
> - **Local / `dotnet run` direto:** o `appsettings.json` já traz o mesmo `Issuer`
>   (`FCG.Catalog.Api`)/`Audience` (`FCG.Catalog.Client`) do Catálogo; a `SecretKey` **não** fica
>   mais no arquivo — você precisa configurar a mesma chave nos dois serviços (via
>   `user-secrets` ou variável de ambiente, ver seção 4).
> - **Kubernetes (AKS):** a pipeline cria o Secret `users-secret` com o `Jwt__SecretKey` vindo da
>   variável `jwt-secret-key` do grupo **compartilhado** `fcg-shared-secrets` — o mesmo grupo que o
>   Catálogo usa, então a chave é a mesma por construção. `k8s/configmap.yaml` injeta
>   `Jwt__Issuer`/`Jwt__Audience` alinhados com o Catálogo.
> - **`docker-compose` (repositório de orquestração):** ambos recebem o mesmo `JWT_SECRET_KEY`,
>   mas com `Issuer`/`Audience` diferentes dos usados em k8s/local (`FCG.Usuario.Api`/
>   `FCG.Usuario.Client`) — token gerado nesse ambiente não vale nos outros dois, e vice-versa. É
>   esperado (cada ambiente é uma execução isolada), mas fica registrado como pegadinha de
>   manutenção — ver seção 9.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe **2 cópias (réplicas)** do container deste serviço, com sondas de saúde (*liveness*/*readiness*) apontando para `/api/health`, limites de CPU/memória e as configurações lidas do ConfigMap e do Secret |
| `service.yaml` | Dá o nome de rede `users-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda só o que **não** é sensível: o `ACCEPT_EULA` do SQL Server e o `Jwt__Issuer`/`Jwt__Audience` (precisam bater com o Catálogo) |
| `secret.yaml.example` | **Modelo** do Secret `users-secret`, com valores de exemplo (`<senha>`, etc.). Mostra quais chaves existem: senha do SQL Server, string de conexão, chave do JWT e todas as configurações do RabbitMQ |
| `sqlserver-deployment.yaml` | Sobe o banco de dados SQL Server usado por este serviço, com um **disco persistente** (PVC) |
| `sqlserver-service.yaml` | Dá o nome de rede `users-sqlserver` para este serviço achar o banco |

### De onde vem o Secret

O arquivo `k8s/secret.yaml` com os valores reais **não fica no Git** (está no `.gitignore`).

- **No AKS (produção):** quem cria o Secret é a própria pipeline do Azure DevOps, a cada deploy,
  usando os valores guardados nos *Variable Groups* (ver seção 8). Ninguém precisa mexer em
  arquivo nenhum.
- **Localmente (ex.: Kubernetes do Docker Desktop):** copie o modelo, preencha os valores e aplique:

```bash
cp k8s/secret.yaml.example k8s/secret.yaml   # depois edite e preencha os valores reais
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml -f k8s/sqlserver-deployment.yaml -f k8s/sqlserver-service.yaml -f k8s/service.yaml -f k8s/deployment.yaml
```

### Atualização sem tirar o serviço do ar (Rolling Update)

Quando sai uma versão nova, o Kubernetes não desliga tudo de uma vez. Ele faz uma **troca
gradual** (*Rolling Update*), configurada no `deployment.yaml` com `maxSurge: 1` e
`maxUnavailable: 0`:

1. Sobe **1 cópia nova** a mais (além das 2 que já estão rodando).
2. Espera essa cópia nova responder "estou pronta" na sonda `/api/health`.
3. Só então desliga 1 cópia antiga — e repete até todas serem novas.

Assim sempre há pelo menos 2 cópias atendendo. Se a versão nova não ficar pronta, as antigas
continuam no ar.

### O disco do banco (PVC)

O SQL Server guarda os dados num **disco persistente** (*PersistentVolumeClaim* de 5 GiB). No AKS
isso vira um disco gerenciado do Azure; ou seja, **os dados não se perdem** se o pod do banco
reiniciar. Dois detalhes técnicos:

- O banco usa a estratégia `Recreate` (e não Rolling Update), porque o disco só pode ser usado por
  um pod por vez: o pod antigo é desligado antes de o novo subir.
- O `fsGroup: 10001` dá ao usuário interno do SQL Server (`mssql`) permissão para escrever no disco.

> **Pré-requisito:** um RabbitMQ precisa já estar disponível no endereço informado em
> `RABBITMQ__HOST` (no Secret). Pode ser o RabbitMQ que roda no próprio cluster, com o nome de
> Service `rabbitmq` (definido no repositório [`FCG.Orchestration`](../FCG.Orchestration)), ou um
> RabbitMQ externo na nuvem.

---

## 7. Testes

O projeto `FCG.Usuario.Tests` traz **17 testes unitários automatizados** (usando xUnit, Moq e
coverlet para medir a cobertura). São testes "de unidade": verificam as regras de negócio
isoladamente, **sem precisar de banco de dados nem RabbitMQ** — o acesso a dados e o envio de
eventos são substituídos por "dublês" (*mocks*).

| Arquivo | O que é verificado |
|---|---|
| `Services/UsuarioServiceTests.cs` | Cadastro de usuário novo (salva e devolve o ID); a senha **nunca é guardada em texto puro** (é salva como hash BCrypt); e-mail já cadastrado não cria outra conta; pedido vazio gera erro; login com dados corretos devolve o token; login com senha errada ou e-mail inexistente é recusado |
| `Services/JwtServiceTests.cs` | O token gerado contém os dados do usuário (*claims*); usa o `Issuer`, `Audience` e tempo de expiração configurados; usuário nulo gera erro |
| `Validators/CriarUsuarioValidatorTests.cs` | Regras do formulário de cadastro: dados válidos passam; senha fraca é recusada com a mensagem certa (menos de 8 caracteres, sem letra, sem número ou sem caractere especial); e-mail inválido; nome vazio ou longo demais |

Para rodar todos os testes:

```bash
dotnet test FCG.Usuario.slnx
```

Os mesmos testes rodam automaticamente na pipeline de CI/CD a cada mudança (ver seção 8).

---

## 8. CI/CD com Azure DevOps

**CI/CD** é a "esteira automática" que leva o código do repositório até o ar. Neste serviço ela é
definida no arquivo `azure-pipelines.yml` e roda no **Azure DevOps**, publicando no **AKS**
(o Kubernetes gerenciado do Azure).

```
Pull Request ──► Build & Test  (só valida, não publica nada)

push na main ──► Build & Test ──► Imagem (build + scan + push no ACR) ──► Deploy no AKS
```

### As três etapas

1. **Build & Test** — compila o projeto e roda os testes unitários (seção 7). Os resultados dos
   testes e a **cobertura de código** ficam publicados na própria execução da pipeline. Essa
   etapa roda também em todo *Pull Request*, para pegar erros **antes** de juntar o código na `main`.
2. **Imagem** — empacota o serviço em uma imagem Docker, passa um **scanner de vulnerabilidades**
   (Trivy, procurando falhas de gravidade alta ou crítica) e envia a imagem para o **ACR** (Azure
   Container Registry, o "depósito" de imagens). Cada execução gera uma etiqueta única — o número
   da execução (`Build.BuildId`) —, então dá para saber exatamente qual versão está no ar.
3. **Deploy no AKS** — roda no *environment* `fcg-production` do Azure DevOps (que guarda o
   histórico de deploys e permite exigir aprovação manual). Ela:
   - cria/atualiza o Secret `users-secret` com os valores dos *Variable Groups*;
   - aplica o ConfigMap, o banco SQL Server e os Services;
   - troca a imagem `fcg-users-api:latest` do `deployment.yaml` pela imagem recém-publicada
     (`<acr>.azurecr.io/fcg-users-api:<BuildId>`), o que dispara o **Rolling Update** (seção 6);
   - espera até 5 minutos todas as cópias novas ficarem prontas (`kubectl rollout status`).

As etapas Imagem e Deploy **não rodam** em Pull Requests.

### Rollback automático

Se as cópias novas não ficarem prontas (por exemplo, a versão nova trava ao iniciar), a etapa de
deploy falha e a pipeline executa automaticamente `kubectl rollout undo`, que **volta para a
versão anterior**. Como o Rolling Update nunca derruba uma cópia antiga antes de a nova estar
pronta, o serviço continua no ar durante todo esse processo.

### Nenhuma credencial no código

Todas as senhas e chaves ficam guardadas no Azure DevOps, em **Pipelines > Library > Variable
groups**, marcadas como secretas (ou ligadas a um Azure Key Vault):

| Variable group | Variáveis |
|---|---|
| `fcg-shared-secrets` (compartilhado com os outros serviços) | `jwt-secret-key`, `rabbitmq-host`, `rabbitmq-vhost`, `rabbitmq-port`, `rabbitmq-use-ssl`, `rabbitmq-user`, `rabbitmq-password` |
| `fcg-users-secrets` (só deste serviço) | `users-sql-password` |

A pipeline lê esses valores e cria o Secret do Kubernetes na hora do deploy — eles nunca passam
pelo Git. Além dos segredos, o arquivo tem alguns nomes de infraestrutura que precisam ser
ajustados para o ambiente de cada um: `azureServiceConnection` (`fcg-azure`), `acrName` (`fcgacr`),
`aksResourceGroup` (`rg-fcg`) e `aksClusterName` (`aks-fcg`).

---

## 9. Limitações conhecidas

- A senha é protegida com **hash BCrypt**, mas o login tem um pequeno *timing side-channel*: quando
  o e-mail não existe, a resposta sai um pouco mais rápido (o código retorna antes de chamar o
  BCrypt), enquanto uma senha errada passa pela verificação BCrypt completa. Em teoria, medir esse
  tempo poderia revelar se um e-mail está cadastrado. É de baixa severidade e não foi corrigido —
  fica registrado por transparência.
- Os testes automatizados são só **unitários** (seção 7): não há testes de integração que subam
  um banco SQL Server ou um RabbitMQ de verdade.
- Senhas e chaves não ficam mais no Git, mas dentro do cluster o Secret do Kubernetes é guardado
  apenas em base64, que **não é criptografia** — quem tiver acesso de leitura aos Secrets do
  cluster consegue ler os valores.
- O scanner de vulnerabilidades (Trivy) na pipeline está em modo **só relatório**
  (`--exit-code 0`): ele lista as falhas encontradas, mas não impede a publicação da imagem.
- Este serviço (`Program.cs:56-57`) desabilita `ValidateIssuer`/`ValidateAudience` para validar
  tokens em seus **próprios** endpoints protegidos (ex: `/api/health/secure`) — só confere
  assinatura e validade. O Catálogo, por outro lado, valida os dois. Não é uma falha de segurança
  em si (a assinatura ainda garante que o token veio de uma fonte com a chave certa), mas é uma
  inconsistência de postura entre os serviços da mesma família.
- **`Issuer`/`Audience` divergem entre ambientes** (ver nota da seção 5): em k8s/local o Usuario se
  identifica como `FCG.Catalog.Api` (nome do serviço *validador*, não do emissor) — funciona porque
  é apenas *string matching* contra o que o Catálogo espera, mas é confuso de manter. Recomendado
  padronizar para `FCG.Usuario.Api`/`FCG.Usuario.Client` em todos os ambientes numa próxima limpeza.
- No Kubernetes, o banco SQL Server roda **dentro do cluster, com uma única cópia** (os dados ficam
  num disco persistente, mas não há réplica do banco). Durante uma atualização do banco (estratégia
  `Recreate`) ou se o pod dele cair, a API fica sem banco por alguns instantes. Em produção real,
  o indicado seria um banco gerenciado (por exemplo, Azure SQL).
- Este serviço apenas **publica** o `UserCreatedEvent`; ele não confirma se a Notificação foi de
  fato processada. Se o RabbitMQ estiver indisponível no momento da publicação, o aviso de
  boas-vindas pode se perder (não há *outbox* nem retentativa persistente).

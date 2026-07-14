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
├── k8s/                            # manifestos de deploy no Kubernetes (ver seção 6)
└── Dockerfile                      # receita para empacotar este serviço em um container
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ e um SQL Server disponíveis —
útil para testar sem precisar subir o projeto inteiro.

### Opção A — via .NET direto (para quem tem o SDK instalado)

```bash
dotnet run --project FCG.Usuario.WebApi
```

### Opção B — via Docker

```bash
docker build -t fcg-users-api:latest .
docker run --rm \
  -e RABBITMQ__HOST=host.docker.internal \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal,1433;Database=users;User Id=sa;Password=Users@Strong!Pass1;TrustServerCertificate=True" \
  -p 5001:8080 \
  fcg-users-api:latest
```

> Para rodar o sistema completo (com RabbitMQ, o banco de dados e o serviço de Notificações
> juntos, prontos para o fluxo de cadastro funcionar de ponta a ponta), use o `docker-compose.yml`
> do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

| Variável | Descrição | Valor padrão (local) |
|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | `localhost` |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | `/` |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` |
| `ConnectionStrings__DefaultConnection` | Endereço e credenciais do banco de dados SQL Server | `Server=users-sqlserver,1433;Database=users;...` |
| `Jwt__SecretKey` | Chave secreta usada para assinar o "crachá digital" (JWT) do usuário | `FCG_USUARIO_SECRET_KEY_2026_SUPER_SECRETA` |
| `Jwt__Issuer` | Quem emite o token (carimbado no JWT) | `FCG.Usuario.Api` |
| `Jwt__Audience` | Para quem o token se destina (carimbado no JWT) | `FCG.Usuario.Client` |

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

> **Acoplamento importante com o Catálogo:** o Catálogo valida os tokens JWT emitidos por este
> serviço, e para isso os dois precisam usar **exatamente a mesma** `Jwt__SecretKey` /
> `JwtSettings__SecretKey`. No `docker-compose.yml` do repositório de orquestração, ambos recebem
> o mesmo valor compartilhado (`tech-challenge-fase-2-fcg-chave-secreta-jwt-256bits-minimo`, via a
> variável `JWT_SECRET_KEY`), então funcionam juntos. Atenção: o valor padrão do `appsettings.json`
> deste serviço (`FCG_USUARIO_SECRET_KEY_2026_SUPER_SECRETA`) é **diferente** do padrão do Catálogo
> — logo, tokens gerados rodando os dois serviços "no braço" (`dotnet run`, sem sobrescrever a
> chave) não serão aceitos pelo Catálogo. Rodar via `docker-compose` alinha os dois automaticamente.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe o container deste serviço, com sondas de saúde (*liveness*/*readiness*) apontando para `/api/health` |
| `service.yaml` | Dá o nome de rede `users-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda o endereço e vhost do RabbitMQ e o `ACCEPT_EULA` do SQL Server (não são segredos) |
| `secret.yaml` | Guarda usuário/senha do RabbitMQ, a string de conexão do banco, a chave do JWT e a senha do SQL Server (dados sensíveis) |
| `sqlserver-deployment.yaml` | Sobe o banco de dados SQL Server usado por este serviço |
| `sqlserver-service.yaml` | Dá o nome de rede `users-sqlserver` para este serviço achar o banco |

Para aplicar:

```bash
kubectl apply -f k8s/
```

> **Pré-requisito:** o RabbitMQ precisa já estar rodando no cluster com o nome de Service
> `rabbitmq` — ele é definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration).

---

## 7. Testes

Este serviço **ainda não possui um projeto de testes automatizados** — não há um
`FCG.Usuario.Tests` neste repositório (diferente do Catálogo, Pagamentos e Notificações, que têm).
Por enquanto, a verificação é feita manualmente pelos endpoints (por exemplo, via Swagger).

## 8. Limitações conhecidas

- A senha é protegida com **hash BCrypt**, mas o login tem um pequeno *timing side-channel*: quando
  o e-mail não existe, a resposta sai um pouco mais rápido (o código retorna antes de chamar o
  BCrypt), enquanto uma senha errada passa pela verificação BCrypt completa. Em teoria, medir esse
  tempo poderia revelar se um e-mail está cadastrado. É de baixa severidade e não foi corrigido —
  fica registrado por transparência.
- É o **único serviço da família sem testes automatizados** (ver seção 7).
- Os valores padrão da chave JWT **divergem entre as fontes de configuração** (`appsettings.json`,
  `k8s/secret.yaml` e `docker-compose.yml`). Rodando via `docker-compose`, tudo é alinhado com o
  Catálogo; rodando isolado com os padrões do `appsettings.json`, os tokens não serão aceitos pelo
  Catálogo (ver a nota da seção 5).
- No Kubernetes, o banco SQL Server usa um volume `emptyDir` — ou seja, **os dados são perdidos se
  o pod do banco reiniciar**. Serve para demonstração, não para produção.
- Este serviço apenas **publica** o `UserCreatedEvent`; ele não confirma se a Notificação foi de
  fato processada. Se o RabbitMQ estiver indisponível no momento da publicação, o aviso de
  boas-vindas pode se perder (não há *outbox* nem retentativa persistente).

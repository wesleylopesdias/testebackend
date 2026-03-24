# Shipay Back-End Engineer Challenge - Questao 1

## Resumo

API sincrona em .NET 8 para validar cadastro de clientes a partir de CNPJ e CEP.

Principais pontos da implementacao:

- arquitetura em camadas com `Api`, `Application`, `Domain` e `Infrastructure`
- consulta de CNPJ na BrasilAPI
- consulta de CEP com provedor primario configuravel e fallback automatico
- retries, timeout e circuit breaker para falhas transientes
- cache em memoria para respostas validas
- testes unitarios, de integracao, de arquitetura e de performance

## Estrutura

Solucao publicada na pasta do desafio:

- `tech-challenges/back_end/teahupoo/CnpjCepValidation.sln`

Projetos:

- `src/CnpjCepValidation.Api`
- `src/CnpjCepValidation.Application`
- `src/CnpjCepValidation.Domain`
- `src/CnpjCepValidation.Infrastructure`
- `tests/CnpjCepValidation.Unit`
- `tests/CnpjCepValidation.Integration`
- `tests/CnpjCepValidation.Architecture`
- `tests/CnpjCepValidation.Performance`

## Endpoint

`POST /api/v1/customer-registration-validations`

Exemplo de payload:

```json
{
  "cnpj": "00924432000199",
  "cep": "13288190"
}
```

Retornos:

- `200` quando UF, cidade e logradouro coincidirem
- `404` quando nao coincidirem ou quando CNPJ/CEP nao forem encontrados
- `400` para payload invalido
- `503` quando as dependencias externas estiverem indisponiveis

## Execucao

Na raiz do workspace:

```powershell
dotnet run --project src\CnpjCepValidation.Api\CnpjCepValidation.Api.csproj
```

Para executar em uma porta fixa:

```powershell
dotnet run --project src\CnpjCepValidation.Api\CnpjCepValidation.Api.csproj -- --urls http://127.0.0.1:5079
```

Swagger:

```text
http://localhost:5000/swagger
```

Execucao com Docker:

```powershell
docker build -f tech-challenges\back_end\teahupoo\Dockerfile -t cnpj-cep-validation .
docker run --rm -p 8080:8080 cnpj-cep-validation
```

Health checks no container:

```text
http://localhost:8080/health/live
http://localhost:8080/health/ready
```

Swagger no container somente em `Development`:

```powershell
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development cnpj-cep-validation
```

```text
http://localhost:8080/swagger
```

Execucao com Docker Compose:

```powershell
docker compose up --build
```

Para executar em background:

```powershell
docker compose up --build -d
```

Para encerrar:

```powershell
docker compose down
```

## Configuracao

Opcoes em `appsettings.json`:

- `Validation:PrimaryCepProvider` com valores `BrasilApi` ou `ViaCep`
- `Validation:TimeoutMs`
- `Validation:CnpjRetryCount`
- `Validation:CepPrimaryRetryCount`
- `Validation:CepSecondaryRetryCount`
- `Validation:CacheTtlMinutes`

O arquivo `compose.yaml` aceita sobrescrita via variaveis de ambiente. Exemplo de `.env` na raiz:

```dotenv
API_PORT=8081
ASPNETCORE_ENVIRONMENT=Development
VALIDATION__PRIMARY_CEP_PROVIDER=ViaCep
VALIDATION__TIMEOUT_MS=1200
```

Depois:

```powershell
docker compose up --build -d
```

## Testes

Executar a solucao principal:

```powershell
dotnet test CnpjCepValidation.sln
```

Executar a solucao publicada na pasta do desafio:

```powershell
dotnet test tech-challenges\back_end\teahupoo\CnpjCepValidation.sln
```

Executar somente o teste de performance:

```powershell
dotnet test tests\CnpjCepValidation.Performance\CnpjCepValidation.Performance.csproj
```

## Observacoes

- A comparacao considera UF, cidade e logradouro apos normalizacao.
- Os testes de integracao usam stubs HTTP para garantir determinismo.
- Em chamadas reais, os provedores publicos podem oscilar e retornar `503`.
- Em containers, o ambiente padrao e `Production`; para expor `/swagger`, defina `ASPNETCORE_ENVIRONMENT=Development`.

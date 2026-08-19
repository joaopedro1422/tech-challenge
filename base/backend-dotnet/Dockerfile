FROM mcr.microsoft.com/dotnet/sdk:10.0.100 AS build
WORKDIR /src
COPY Desafio.sln .
COPY src/Desafio.Api/Desafio.Api.csproj src/Desafio.Api/
RUN dotnet restore src/Desafio.Api/Desafio.Api.csproj
COPY src/ src/
RUN dotnet publish src/Desafio.Api/Desafio.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
RUN apk add --no-cache icu-libs
COPY --from=build /app .
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Desafio.Api.dll"]
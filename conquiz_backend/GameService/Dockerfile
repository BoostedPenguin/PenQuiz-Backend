FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet build -c Release --no-restore
RUN dotnet test -c Release --no-build
RUN dotnet publish -c Release --no-build -o out

FROM mcr.microsoft.com/dotnet/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT [ "dotnet", "GameService.dll" ]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/iLearning.Web/iLearning.Web.csproj backend/iLearning.Web/
RUN dotnet restore backend/iLearning.Web/iLearning.Web.csproj

COPY backend/iLearning.Web/. backend/iLearning.Web/
WORKDIR /src/backend/iLearning.Web
RUN dotnet publish iLearning.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 8080
CMD ["sh", "-c", "dotnet iLearning.Web.dll --urls http://0.0.0.0:${PORT:-8080}"]

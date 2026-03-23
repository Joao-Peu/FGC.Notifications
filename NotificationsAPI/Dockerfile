FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["NotificationsAPI/NotificationsAPI.csproj", "NotificationsAPI/"]
RUN dotnet restore "NotificationsAPI/NotificationsAPI.csproj"
COPY NotificationsAPI/ NotificationsAPI/
RUN dotnet publish "NotificationsAPI/NotificationsAPI.csproj" -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot

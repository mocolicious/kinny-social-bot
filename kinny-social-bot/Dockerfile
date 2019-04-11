FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["kinny-social-bot/kinny-social-bot.csproj", "kinny-social-bot/"]
COPY ["kinny-social-core/kinny-social-core.csproj", "kinny-social-core/"]
RUN dotnet restore "kinny-social-bot/kinny-social-bot.csproj"
COPY . .
WORKDIR "/src/kinny-social-bot"
RUN dotnet build "kinny-social-bot.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "kinny-social-bot.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "kinny-social-bot.dll"]
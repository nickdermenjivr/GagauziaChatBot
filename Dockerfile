# Используем официальный образ .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем проект
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код и собираем
COPY . ./
RUN dotnet publish -c Release -o /out

# Используем рантайм-образ для запуска
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Запускаем бота
CMD ["dotnet", "GagauziaChatBot.dll"]

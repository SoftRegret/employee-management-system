# Multi-stage Build: compile with the SDK and copy published output into a runtime-only image.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["EmployeeManagement.Api.csproj", "./"]
RUN dotnet restore "EmployeeManagement.Api.csproj"

COPY . .

RUN dotnet publish "EmployeeManagement.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "EmployeeManagement.Api.dll"]
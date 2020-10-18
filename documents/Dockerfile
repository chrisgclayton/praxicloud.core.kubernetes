FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /app

# Copy Solution Directory
COPY . .
RUN dotnet restore ./src/praxicloud.core.kubernetes.solution.sln

# copy and publish app and libraries
WORKDIR /app/
COPY /. ./
WORKDIR /app
RUN dotnet publish -c Release -o out ./src/praxicloud.core.kubernetes.testcontainer/praxicloud.core.kubernetes.testcontainer.csproj

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "praxicloud.core.kubernetes.testcontainer.dll"]

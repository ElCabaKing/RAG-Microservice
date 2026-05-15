# Build image using .NET 9 SDK
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and restore
COPY . ./
ENV DOTNET_ROLL_FORWARD=LatestPatch
RUN dotnet --info
# Remove global.json inside the build container to avoid exact SDK version pin causing restore failure
RUN rm -f /src/global.json || true
RUN dotnet restore "src/Rag.Api/Rag.Api.csproj"

# Publish the API project
RUN dotnet publish "src/Rag.Api/Rag.Api.csproj" -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "Rag.Api.dll"]

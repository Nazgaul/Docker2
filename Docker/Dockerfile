#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
ENV DEBIAN_FRONTEND noninteractive
RUN apt-get update
RUN apt-get install -y xorg gtk2-engines-pixbuf dbus-x11 xfonts-base xfonts-100dpi xfonts-75dpi xfonts-cyrillic xfonts-scalable
RUN apt-get install -y xvfb ffmpeg x11-apps x11-utils
RUN apt-get install -y pulseaudio
RUN apt-get install -y wget
RUN apt-get install -y psmisc
RUN apt-get install -y gnupg



RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list'
RUN apt-get update && apt-get install -y google-chrome-stable
ENV DISPLAY :99
WORKDIR /app



FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Docker/Docker.csproj", "Docker/"]
RUN dotnet restore "Docker/Docker.csproj"
COPY . .
WORKDIR "/src/Docker"
RUN dotnet build "Docker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Docker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docker.dll"]

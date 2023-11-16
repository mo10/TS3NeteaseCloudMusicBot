FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build

RUN sed -i 's/deb.debian.org/mirrors.ustc.edu.cn/g' /etc/apt/sources.list && \
    apt-get update && \
    apt-get install -y unzip && \
    rm -rf /var/lib/apt/lists/* && \
    mkdir -p /app && \
    wget https://github.com/Splamy/TS3AudioBot/releases/download/0.12.0/TS3AudioBot_dotnetcore3.1.zip -o /tmp/ts3audiobot.zip && \
    unzip /tmp/ts3audiobot.zip -d /app/TS3AudioBot_dotnetcore3.1 && \
    rm -rf /tmp/ts3audiobot.zip \
    

WORKDIR /app/ts3ncm

COPY . /app/ts3ncm

RUN dotnet restore && \
    dotnet build -c Release -f netcoreapp3.1 -o out

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS runtime

RUN sed -i 's/deb.debian.org/mirrors.ustc.edu.cn/g' /etc/apt/sources.list && \
    apt-get update && \
    apt-get install -y libopus-dev ffmpeg && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/TS3AudioBot_dotnetcore3.1 ./
COPY --from=build /app/ts3ncm/out/ts3ncm.dll ./plugins

EXPOSE 58913

ENTRYPOINT ["dotnet", "TS3AudioBot.dll"]
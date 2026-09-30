# The image every `runs-on: docker` job gets.
ARG DOTNET_SDK=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK}

# Node, because actions/checkout and every other JavaScript action runs on the
# node in the JOB's image, not the runner's. Track the version chezmoi installs
# on the node (platform/chezmoi/home/.chezmoiexternal.toml.tmpl).
ARG NODE_VERSION=22.23.2
RUN set -eux; \
    case "$(dpkg --print-architecture)" in \
      amd64) arch=x64 ;; \
      arm64) arch=arm64 ;; \
      *) echo "unsupported architecture: $(dpkg --print-architecture)" >&2; exit 1 ;; \
    esac; \
    apt-get update; \
    apt-get install -y --no-install-recommends xz-utils; \
    rm -rf /var/lib/apt/lists/*; \
    cd /tmp; \
    curl -fsSLO "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-${arch}.tar.xz"; \
    curl -fsSLO "https://nodejs.org/dist/v${NODE_VERSION}/SHASUMS256.txt"; \
    grep " node-v${NODE_VERSION}-linux-${arch}.tar.xz$" SHASUMS256.txt | sha256sum -c -; \
    tar -xJf "node-v${NODE_VERSION}-linux-${arch}.tar.xz" -C /usr/local --strip-components=1 \
        --exclude CHANGELOG.md --exclude LICENSE --exclude README.md; \
    rm -f node-v*.tar.xz SHASUMS256.txt; \
    node --version

# Docker's CLI and the compose plugin, from whichever distro the SDK image is
# built on — it is Ubuntu today and was Debian before, so the repository is
# derived rather than named. No daemon is needed: `docker compose
# config` resolves and validates the file locally, which is why a compose check
# can run here while the runner keeps `docker_host: "-"` and hands no job the
# host's socket.
RUN set -eux; \
    . /etc/os-release; \
    install -m 0755 -d /etc/apt/keyrings; \
    curl -fsSL "https://download.docker.com/linux/${ID}/gpg" -o /etc/apt/keyrings/docker.asc; \
    chmod a+r /etc/apt/keyrings/docker.asc; \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/${ID} ${VERSION_CODENAME} stable" \
        > /etc/apt/sources.list.d/docker.list; \
    apt-get update; \
    apt-get install -y --no-install-recommends docker-ce-cli docker-compose-plugin; \
    rm -rf /var/lib/apt/lists/*; \
    docker --version; \
    docker compose version

RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends jq rsync bzip2; \
    rm -rf /var/lib/apt/lists/*; \
    jq --version; \
    rsync --version | head -n 1

# The lab's CLI, `lab`, at the version its pin names, so a job runs the LAST, 
# because it moves most often: a bump rebuilds from the COPY down, nothing above it.
COPY .config/dotnet-tools.json /tmp/dotnet-tools.json
RUN set -eux; \
    version="$(jq -er '.tools["wolfe.lab"].version' /tmp/dotnet-tools.json)"; \
    dotnet tool install --tool-path /opt/lab Wolfe.Lab \
      --version "${version}" \
      --add-source https://code.twolfe.dev/api/packages/tom-wolfe/nuget/index.json; \
    /opt/lab/lab --help > /dev/null; \
    rm /tmp/dotnet-tools.json
ENV PATH="/opt/lab:${PATH}"

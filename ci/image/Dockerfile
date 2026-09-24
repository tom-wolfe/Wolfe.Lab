# The image every `runs-on: docker` job gets.
ARG DOTNET_SDK=10.0
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK}

# Node, because actions/checkout and every other JavaScript action runs on the
# node in the JOB's image, not the runner's. Track the version chezmoi installs
# on the node (chezmoi/home/.chezmoiexternal.toml.tmpl).
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

# shellcheck and chezmoi, for the chezmoi slice's check: every profile renders,
# and what it renders is valid shell. chezmoi pinned to what the Brewfile
# installs on the nodes; bump both together.
ARG CHEZMOI_VERSION=2.72.2
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends shellcheck; \
    rm -rf /var/lib/apt/lists/*; \
    sh -c "$(curl -fsLS get.chezmoi.io)" -- -b /usr/local/bin -t "v${CHEZMOI_VERSION}"; \
    chezmoi --version; \
    shellcheck --version

# The lab's CLI, `lab`, at the version its pin names, so a job runs the
# published, reviewed CLI rather than compiling the checkout. LAST, because
# it moves most often: a bump rebuilds this layer and nothing above it. Bump
# together with the version in chezmoi's install-lab script, which pins the
# host runners to the same one.
ARG LAB_VERSION=0.40.1
RUN set -eux; \
    dotnet tool install --tool-path /opt/lab Wolfe.Lab.Build \
      --version "${LAB_VERSION}" \
      --add-source https://code.twolfe.dev/api/packages/tom-wolfe/nuget/index.json; \
    /opt/lab/lab --help > /dev/null
ENV PATH="/opt/lab:${PATH}"

# immich-go for the Takeout import, from the pinned release: it ships no
# image, and the lab installs nothing on the node. Built by `lab import`.
FROM alpine:3.22

ARG VERSION=0.32.0
ARG SHA256=2c35d9284baae407ef9540bdac5f488971b0bdc7be758a4d7c05ab270af09fdb

RUN apk add --no-cache ca-certificates \
 && wget -qO /tmp/immich-go.tar.gz "https://github.com/simulot/immich-go/releases/download/v${VERSION}/immich-go_Linux_arm64.tar.gz" \
 && echo "${SHA256}  /tmp/immich-go.tar.gz" | sha256sum -c - \
 && tar -xzf /tmp/immich-go.tar.gz -C /usr/local/bin immich-go \
 && rm /tmp/immich-go.tar.gz

# Runs as the host user, which has no passwd entry here and so no home;
# immich-go keeps a cache under ~, so its home is the scratch directory.
# The takeout is mounted read-only; logs go to the working directory.
WORKDIR /tmp
ENV HOME=/tmp XDG_CACHE_HOME=/tmp/.cache
USER 501:20
ENTRYPOINT ["immich-go"]

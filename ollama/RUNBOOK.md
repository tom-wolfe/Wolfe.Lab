# ollama runbook

## Pull a model

Models are not in the repo and not in the backups. Pulling one is a
manual act on the node, because which model the lab runs is a decision
rather than a deployment.

    ssh macmini.local
    ollama pull <model>
    ollama list

## Check it is serving

From the mini itself, the server and the models it can answer with:

    curl -s http://127.0.0.1:11434/api/tags

From anywhere on the tailnet, the same through the front door — this is
the path the lab's own jobs take, so it is the one worth trusting:

    curl -s https://ai.twolfe.dev/api/tags

A 502 from the second with the first working means caddy cannot reach the
host process: check `OLLAMA_HOST` is `0.0.0.0` and not loopback.

## Check the agent

What launchd thinks, including the last exit status:

    launchctl print gui/$(id -u)/dev.twolfe.ollama

Its output, both streams:

    tail -f ~/Library/Logs/ollama.log

## Change how it runs

Edit `ritten.json` and push. The converge job rewrites the unit and
restarts the agent only if the rendered unit actually changed, so a
re-run that changes nothing is a no-op.

To rehearse the change first, from the slice directory on the node:

    dotnet run --project ../build/src/Wolfe.Lab.Build -- converge --dry-run

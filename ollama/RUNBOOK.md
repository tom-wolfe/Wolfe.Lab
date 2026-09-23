# ollama runbook

## First run: approve the file-access prompt

Once per machine, before the agent can work. ollama reads the model store
on `/Volumes/Data2`, which macOS gates behind a privacy prompt that a
launchd agent cannot display — it blocks forever instead. Trigger the
prompt from a session that CAN show it, at the mini itself:

    ssh macmini.local
    ollama serve

Approve the access prompt on the mini's screen, then stop it with ctrl-C.
The grant is recorded for the binary, so the agent gets it too:

    launchctl kickstart -k gui/$(id -u)/dev.twolfe.ollama
    curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:11434/api/tags

200 means it is serving. A hang means the prompt was not approved, or was
approved for a different binary than the agent runs.

## Change which models the node holds

Models are declared, not pulled by hand: the `models.pull` list in
`ritten.json` is what the node should have, and `lab deploy` fetches
whatever is missing. Add or remove a line and push.

A model must name its tag — `qwen3:8b`, never `qwen3` — so the version
the lab runs is the version it declared.

Removing a line does NOT delete the model. The deploy reports anything on
the node it did not ask for and leaves it alone; several gigabytes that
somebody pulled on purpose is not something a config file should silently
bin. To reclaim the space, at the desk:

    ssh macmini.local
    ollama list
    ollama rm <model>

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

Edit `ritten.json` and push. The deploy job rewrites the unit and
restarts the agent only if the rendered unit actually changed, so a
re-run that changes nothing is a no-op.

To rehearse the change first, from `ollama/server` on the node:

    dotnet run --project ../../build/src/Wolfe.Lab.Build -- deploy --dry-run

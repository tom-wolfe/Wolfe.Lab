#!/bin/bash

forgeBase="ssh://git@git.twolfe.dev"

for dir in "$1"/*/*/; do
  org=$(dirname "$dir")
  org=$(basename "$org")
  name=$(basename "$dir")
  echo "$forgeBase/$org/$name.git"
  git -C "$dir" remote remove origin
  git -C "$dir" remote add origin "$forgeBase/$org/$name.git"
  git -C "$dir" push origin
done
#!/bin/bash

for dir in /Users/tomwolfe/Development/SIMPLIFi/*/; do
  name=$(basename "$dir")
  echo $name
  git -C "$dir" remote set-url origin "ssh://git@macmini.local:2222/SIMPLIFi/$name.git"
  git -C "$dir" push
done
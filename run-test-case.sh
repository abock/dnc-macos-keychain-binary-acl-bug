#!/bin/bash

set -e
cd "$(cd "$(dirname "$0")"; pwd)"

dotnet build

secret_id=$(/usr/bin/uuidgen)

echo "SECRET ID FOR TEST RUN: $secret_id"

IFS=$'\n'
for ((outeriter=0; outeriter<2; outeriter++)); do
  for published in $(find . -type d -name publish | sort); do
    echo "App: $published"
    for ((iter=0; iter<3; iter++)); do
      echo "  Iter: ${outeriter}.${iter}"
      rm -rf _run
      cp -a "$published" _run
      test -f _run/Sample && _run/Sample $secret_id
    done
    echo
  done
done
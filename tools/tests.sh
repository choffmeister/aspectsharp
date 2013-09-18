#!/bin/bash -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
SRC_DIR="$DIR/../src"

cd $SRC_DIR
xbuild /p:Configuration=Debug /verbosity:quiet AspectSharp.sln
mono --runtime=v4.0 libs/nunit-runners/tools/nunit-console.exe AspectSharp.Tests/bin/Debug/AspectSharp.Tests.dll

exit 0

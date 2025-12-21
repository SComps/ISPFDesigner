#!/bin/bash

echo "Building single-file executable for Linux..."
dotnet publish -c Release --self-contained true /p:PublishSingleFile=true -o Publish

echo "Build complete. Executable is in Publish/"

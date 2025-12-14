#!/bin/bash
echo "Installing dependencies (clang, zlib1g-dev)..."
sudo apt-get update && sudo apt-get install -y clang zlib1g-dev

echo "Building single-file executable for Linux..."
dotnet publish -c Release --self-contained true /p:PublishSingleFile=true -o Publish

echo "Build complete. Executable is in Publish/"

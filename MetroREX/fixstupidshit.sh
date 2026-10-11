mkdir -p ~/.local/bin
ln -sf ~/.dotnet/dotnet ~/.local/bin/dotnet
hash -r
which dotnet

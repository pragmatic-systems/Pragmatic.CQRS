# AGENTS.md

## NuGet cache corruption (recurring)

Restore/build errors like `NU1102`/`NU1103`/`NU1403`, `NU1006`, "not a valid NuGet package", or SHA512 hash mismatches usually mean a corrupted global packages folder — recurring issue due to shared Windows host + Linux container domain. 
Don't patch around it: Wait for IDE/NCrunch file locks to release, then run `dotnet nuget locals global-packages --clear`, `dotnet nuget locals temp --clear`, `dotnet nuget locals http-cache --clear`, and rebuild with `dotnet cake --Target=BuildAndTest`. 
If `NUGET_PACKAGES` is set, the cache lives there instead of `~/.nuget/packages`.

## Git in the Linux container

If git fails with `fatal: detected dubious ownership`, it's the 9p mount showing files as `root:root` while the agent runs as a different uid — run `git config --global --add safe.directory /home/appuser/mount/Pragmatic.CQRS` (idempotent; needed again after a container rebuild). 
Never happens on the Windows host.

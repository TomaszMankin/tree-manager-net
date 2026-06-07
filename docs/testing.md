# Test tiers — tree-manager-net

## L0 — unit

Zero I/O, zero filesystem, zero network. All external dependencies mocked. Fast.  
Tag: `[Trait("Tier", "L0")]`.

## L1 — integration

Real filesystem under temporary paths. No UI, no network.  
Tag: `[Trait("Tier", "L1")]`.

## L2 — headed UI automation

WPF UI automation against a running app instance. Not yet present.

## e2e — end-to-end

Full app launch, real filesystem, real network. Not yet present.

## CI gate

`dotnet test --filter "Tier=L0"` / `--filter "Tier=L1"`.  
Composite action `.github/actions/run-test-tier` parameterised by `tier`.  
L2 and e2e suites are gated `if: false` until implemented.

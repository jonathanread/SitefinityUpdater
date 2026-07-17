# Upgrade Options — SitefinityContentUpdater

Assessment: 6 projects (all net9.0/net10.0, SDK-style), 2-tier dependency graph, 2 package upgrades recommended, 1 deprecated package (xunit), behavioral API changes in 3 projects

## Strategy

### Upgrade Strategy
All 6 projects are on modern .NET (net9.0/net10.0), SDK-style, with a shallow 2-tier dependency graph and no .NET Framework boundary to cross — a single atomic pass is the lowest-overhead approach.

| Value | Description |
|-------|-------------|
| **All-at-Once** (selected) | Upgrade all projects simultaneously in a single atomic pass — fastest approach, no multi-targeting overhead |
| Top-Down | Upgrade entry-point applications first, temporarily multi-targeting shared libraries; adds overhead not justified for this scope |

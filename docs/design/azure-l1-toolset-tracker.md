# Azure L1 Toolset Tracker

## Purpose

This document tracks the Azure MCP toolsets needed for an Azure Level 1 operations agent. It separates what is already available in the repository from what must be added for L1 diagnostics, controlled remediation, and ITSM-assisted workflows.

The tracker is intentionally aligned to the operational model described in [azure-l1-ops-agent.md](azure-l1-ops-agent.md) and the concrete engineering backlog in [azure-l1-milestone-backlog.md](azure-l1-milestone-backlog.md):

- read-first investigation
- human approval for all write actions
- evidence-based escalation to L2
- safe, composable tool boundaries

---

## Status legend

- Completed: toolset or capability already exists and fits the L1 workflow
- In progress: active implementation in repo or pending repo changes
- Planned: designed and approved but not implemented yet
- Blocked: requires architecture, approval, or dependency work before implementation
- Not started: no work started

---

## Overall maturity

| Area | Status | Notes |
|---|---|---|
| Core Azure MCP platform | Completed | Remote HTTP mode, Entra auth, tool registration model are already in place |
| Read-only diagnostics foundations | Completed | Existing Azure service toolsets cover several monitoring and lookup patterns |
| L1 network troubleshooting | Completed | `Azure.Mcp.Tools.NetworkDiagnostics` implemented (Topology, NSG, Private DNS, Peering) |
| VM/OS health diagnostics | Completed | `Azure.Mcp.Tools.VmDiagnostics` implemented (Health, Metrics, Top Processes, Event Logs) |
| App health and deployment triage | Completed | `Azure.Mcp.Tools.AppDiagnostics` implemented (Health, Deployment Triage, Log Analysis) |
| Human-approved write workflows | Completed | Safe gating and approval model defined in design & SreAgent |
| ITSM ticket evidence integration | Completed | Supported via ServiceNow & PagerDuty connectors in SreAgent |
| AWS-managed orchestration integration | Completed | Remote HTTP endpoint + Entra Bearer token integration ready for container host |

---

## Repository toolset coverage

The following toolsets are already present in the repo and form the baseline for L1 support.

| Toolset | Status | L1 value | Notes |
|---|---|---|---|
| Azure.Mcp.Tools.NetworkDiagnostics | Completed | High | VNet, subnet, NSG rule validation, Private DNS, and peering diagnostics |
| Azure.Mcp.Tools.VmDiagnostics | Completed | High | VM instance view, agent status, metrics review, top processes, and event logs |
| Azure.Mcp.Tools.AppDiagnostics | Completed | High | Application health check, deployment failure triage, and exception trace analysis |
| Azure.Mcp.Tools.ResourceHealth | Completed | High | Azure resource health checks and incident correlation |
| Azure.Mcp.Tools.Insights | Completed | High | Monitor / insights queries and diagnostics |
| Azure.Mcp.Tools.Monitor | Completed | High | Azure Monitor-based operational telemetry |
| Azure.Mcp.Tools.AppService | Completed | High | App Service and web app health checks |
| Azure.Mcp.Tools.FunctionApp | Completed | High | Function App diagnostics and lifecycle checks |
| Azure.Mcp.Tools.Compute | Completed | High | VM-related operations and resource discovery |
| Azure.Mcp.Tools.ContainerApps | Completed | Medium | App platform health and revision checks |
| Azure.Mcp.Tools.Storage | Completed | Medium | Storage resource validation and dependency checks |
| Azure.Mcp.Tools.KeyVault | Completed | Medium | Secret and configuration validation support |
| Azure.Mcp.Tools.Authorization | Completed | Medium | RBAC and access validation |
| Azure.Mcp.Tools.Quota | Completed | Medium | Capacity and limitation validation |
| Azure.Mcp.Tools.AppConfig | Completed | Medium | Config and environment validation |
| Azure.Mcp.Tools.Extension | Completed | Medium | Additional diagnostic and utility actions |

---

## L1 operational toolsets to add

### 1. Network and connectivity

| Toolset / capability | Status | Scope | Priority |
|---|---|---|---|
| network topology summary | Completed | VNet, subnet, route table, NSG, private endpoint review | High |
| private dns validation | Completed | DNS propagation, zone links, record verification | High |
| vnet peering validation | Completed | Peer state, route propagation, connectivity | High |
| network connectivity checks (NSG) | Completed | Reachability, ports, NSG rule evaluation | High |
| ip and route correlation | Planned | Identify source of route drift or blocked traffic | High |

### 2. VM and OS diagnostics

| Toolset / capability | Status | Scope | Priority |
|---|---|---|---|
| vm health summary | Completed | VM status, boot state, availability, extensions | High |
| os event log review | Completed | Windows/Linux event correlation | High |
| process and resource review | Completed | top CPU, memory, active processes | High |
| vm run-command diagnostics | Completed | Safe, policy-gated remote process/health checks | High |
| performance anomaly review | Completed | CPU, memory, disk pressure timeline metrics | High |

### 3. Application and deployment health

| Toolset / capability | Status | Scope | Priority |
|---|---|---|---|
| app health summary | Completed | App Service / Function / Container App health | High |
| deployment failure triage | Completed | release status, config drift, app status correlation | High |
| log correlation | Completed | App logs + platform logs + deployment timeline | High |
| config drift validation | Completed | compare deployed config to expected state | High |
| runtime dependency review | Completed | DB, storage, queue, service dependencies | Medium |

### 4. Controlled write workflows

| Toolset / capability | Status | Scope | Priority |
|---|---|---|---|
| approval-gated remediation | Planned | requirement validation before write | Critical |
| temporary diagnostic change workflow | Planned | add logs / setting for a bounded period | High |
| safe restart workflow | Planned | approved restart for non-critical workloads | High |
| network rule update workflow | Planned | NSG/route/DNS change with validation | Critical |
| rollback plan registration | Planned | rollback evidence before production changes | Critical |

### 5. ITSM and operational evidence

| Toolset / capability | Status | Scope | Priority |
|---|---|---|---|
| incident context reader | Planned | read ticket metadata, impact, ownership | High |
| evidence logger | Planned | append structured findings to ticket | High |
| handoff pack generator | Planned | route to L2 with timeline and symptoms | High |
| approval audit trail | Planned | record approver, reason, and timestamp | Critical |
| closure and resolution notes | Planned | final status and findings summary | Medium |

---

## Implementation roadmap

### Phase 1: foundation for safe diagnostics

- confirm baseline Azure MCP host in remote HTTP + Entra mode
- verify the repo has the needed read-only toolsets for health and monitoring
- validate the operational structure for L1 investigation

Status: In progress

### Phase 2: add L1 network and VM diagnostic tools

- network topology summary
- DNS and private endpoint validation
- VM health and OS event diagnostics
- process and resource consumption review

Status: Planned

### Phase 3: add app health and deployment correlation tools

- app lifecycle and health checks
- deployment failure triage
- log correlation and evidence packaging

Status: Planned

### Phase 4: add controlled write and approval model

- approval gate enforcement
- safe remediation workflow
- rollback plan enforcement
- ITSM audit trail

Status: Planned

### Phase 5: connect to orchestration and ticketing workflows

- AWS-managed orchestration integration
- ITSM API / ticketing adapter
- evidence-based escalation to L2 and operations teams

Status: Planned

---

## Operational readiness checklist

A toolset is considered ready for L1 support only when all of the following are true:

- [ ] read-only investigation is available
- [ ] resource scope and RBAC are explicit
- [ ] output is structured for ticket evidence
- [ ] write actions are blocked unless approved
- [ ] rollback or safe revert plan exists
- [ ] escalation path to L2 is defined
- [ ] command metadata is clear and discoverable
- [ ] remote HTTP + Entra hosting is verified

---

## Current recommendation

The immediate focus should be on the following first toolset bundle:

1. network diagnostics
2. VM health diagnostics
3. app health and deployment triage
4. evidence packaging for ITSM

This gives the L1 agent a defensible, low-risk starting point while preserving the approval model required for any write capability.

---

## Ownership and update process

This tracker should be updated whenever a toolset changes state.

Recommended update rules:

- Completed: update the tracker after the implementation is merged and validated
- In progress: update when design or code work begins
- Planned: add when the capability is accepted into roadmap
- Blocked: note the blocker and the dependency required to proceed

The single source of truth for repository changes remains the code and the design documents. This tracker is a project-level operational summary for stakeholder visibility.

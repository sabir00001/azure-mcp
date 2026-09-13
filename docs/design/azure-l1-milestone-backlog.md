# Azure L1 Ops Agent - Implementation Milestone Backlog

This backlog breaks down the design in [azure-l1-ops-agent.md](azure-l1-ops-agent.md) and the operational tracker in [azure-l1-toolset-tracker.md](azure-l1-toolset-tracker.md) into concrete engineering milestones, epics, and work items.

---

## Overview of Milestones

| Milestone | Focus | Key Deliverables | Expected Outcome |
|---|---|---|---|
| **M1: Remote Host & Security** | Host Azure MCP in Docker with Entra ID | Dockerfile review, HTTP server flags, Entra inbound/outbound auth spec | Remote-accessible, Entra-authenticated Azure MCP endpoint for AWS/external agent |
| **M2: L1 Network Diagnostics** | Network, DNS, and Private Endpoint triage | VNet/Subnet/NSG analyzer, Private DNS link validator, route table inspector | Automated L1 triage for connectivity & routing issues |
| **M3: L1 VM & OS Diagnostics** | VM health, OS logs, and process triage | Boot/Extension status, OS event log analyzer, top CPU/Memory process inspector | Automated triage for VM degradation and OS resource pressure |
| **M4: App & Deployment Correlation** | PaaS app health & release correlation | App Service/Function/ACA health aggregator, deployment failure & log correlator | Automated triage for deployment failures and PaaS runtime issues |
| **M5: Approval Gates & ITSM Evidence** | Safe remediation & ticket integration | Approval enforcement wrapper, ITSM evidence formatter, L2 handoff pack generator | Safe, human-approved write actions with complete ITSM audit trails |

---

## Milestone 1: Remote Host & Container Security Setup

### Epic 1.1: Containerized Azure MCP Server Configuration
- [x] **Task 1.1.1**: Verify Dockerfile and build scripts (`eng/scripts/Build-Docker.ps1`) for remote HTTP deployment.
- [x] **Task 1.1.2**: Define standard environment variable matrix for HTTP transport (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_CLIENT_SECRET` / Managed Identity).
- [x] **Task 1.1.3**: Document server execution flags for container entrypoint: `--transport http --outgoing-auth-strategy UseHostingEnvironmentIdentity` (or `UseOnBehalfOf`).

### Epic 1.2: Entra ID Inbound & Outbound Auth Alignment
- [x] **Task 1.2.1**: Specify Entra ID App Registration requirements (Audience, Scope `Mcp.Tools.ReadWrite` / Role `Mcp.Tools.ReadWrite.All`).
- [x] **Task 1.2.2**: Configure token validation rules for HTTP server endpoints.
- [x] **Task 1.2.3**: Document integration guide for AWS Managed Agent connecting to Azure MCP HTTP endpoint with Entra Bearer token.

---

## Milestone 2: L1 Network & Connectivity Diagnostic Toolset

### Epic 2.1: `Azure.Mcp.Tools.NetworkDiagnostics` Scaffold
- [x] **Task 2.1.1**: Scaffold toolset project under `tools/Azure.Mcp.Tools.NetworkDiagnostics/`.
- [x] **Task 2.1.2**: Implement `INetworkDiagnosticsService` with Azure ARM & Resource Graph query helpers.
- [x] **Task 2.1.3**: Register toolset in `servers/Azure.Mcp.Server/src/Program.cs`.

### Epic 2.2: Diagnostic Commands
- [x] **Task 2.2.1**: `azmcp networkdiagnostics topology summary` - Aggregates VNet, subnets, route tables, NSGs, and connected resources.
- [x] **Task 2.2.2**: `azmcp networkdiagnostics nsg validate` - Evaluates effective security rules for specific source/destination IP, port, and protocol.
- [x] **Task 2.2.3**: `azmcp networkdiagnostics privatedns validate` - Checks Private Endpoint DNS zone links, A record registration, and IP resolution.
- [x] **Task 2.2.4**: `azmcp networkdiagnostics peering check` - Validates VNet peering status, sync state, and transit settings.

---

## Milestone 3: L1 VM & OS Health Diagnostic Toolset

### Epic 3.1: `Azure.Mcp.Tools.VmDiagnostics` Scaffold
- [x] **Task 3.1.1**: Scaffold project under `tools/Azure.Mcp.Tools.VmDiagnostics/`.
- [x] **Task 3.1.2**: Implement `IVmDiagnosticsService` wrapping Azure Compute SDK and VM Run Command APIs.

### Epic 3.2: VM & OS Diagnostic Commands
- [x] **Task 3.2.1**: `azmcp vmdiagnostics health get` - Retrieves VM instance view, provisioning state, agent health, and extension status.
- [x] **Task 3.2.2**: `azmcp vmdiagnostics metrics review` - Aggregated CPU, Memory, Disk IOPS, and Network throughput over 1h/6h/24h.
- [x] **Task 3.2.3**: `azmcp vmdiagnostics process list` - Executes safe policy-restricted script via Run Command to list top CPU/Memory processes.
- [x] **Task 3.2.4**: `azmcp vmdiagnostics eventlogs query` - Fetches critical OS event logs (Windows System/Application logs, Linux syslog/journald).

---

## Milestone 4: App & Deployment Health Correlation Toolset

### Epic 4.1: `Azure.Mcp.Tools.AppDiagnostics` Scaffold
- [x] **Task 4.1.1**: Scaffold project under `tools/Azure.Mcp.Tools.AppDiagnostics/`.
- [x] **Task 4.1.2**: Implement `IAppDiagnosticsService` connecting App Service, Functions, Container Apps, and App Insights SDKs.

### Epic 4.2: Correlation & Triage Commands
- [x] **Task 4.2.1**: `azmcp appdiagnostics health check` - Checks HTTP probe status, container restart counts, App Service availability, and dependency health.
- [x] **Task 4.2.2**: `azmcp appdiagnostics deployment triage` - Correlates recent deployments, pipeline logs, config changes, and failure timestamps.
- [x] **Task 4.2.3**: `azmcp appdiagnostics logs analyze` - Queries App Insights / Log Analytics for top 5xx exceptions and trace anomalies in the last 2 hours.

---

## Milestone 5: Approval-Gated Remediation & ITSM Evidence Engine

### Epic 5.1: Operational Safety & Approval Gates
- [x] **Task 5.1.1**: Create `ApprovalRequired` attribute / metadata decorator for operational write commands.
- [x] **Task 5.1.2**: Implement verification check ensuring an `ApprovalTicketId` and `ApprovedBy` token is supplied before executing any write/remediation command.

### Epic 5.2: ITSM Evidence Formatting
- [x] **Task 5.2.1**: Implement `ItsmEvidenceFormatter` to turn diagnostic result models into Markdown/JSON ticket comments.
- [x] **Task 5.2.2**: Create `azmcp support handoff generate` - Generates standardized L2 escalation package containing timeline, evidence, and preliminary root-cause hypothesis.

---

## Acceptance Criteria for Completion

Each toolset introduced in these milestones must meet the repository's production readiness standard:
1. **Primary Constructors**: Used across all classes.
2. **Flat POCO Options**: Using two-generic command pattern `SubscriptionCommand<TOptions, TResult>` with `[Option]` attributes.
3. **AOT Compatibility**: All models registered in dedicated `JsonSerializerContext`.
4. **Unit Tests**: Full coverage for option validation, service calls, and error handling.
5. **Operation Plane Metadata**: Explicitly declared on `[CommandMetadata]`.
6. **Documentation**: Updated `azmcp-commands.md` and test prompts added to `e2eTestPrompts.md`.

# Azure L1 Ops Agent Design

## Objective

Design an Azure operations agent for Level 1 support that can:

- inspect Azure infrastructure, VM OS health, app health, and deployment state
- correlate symptoms across infrastructure, platform, and application layers
- raise and update ITSM incidents with structured evidence
- perform only approved write operations under human approval
- escalate to L2 with a clear, evidence-based handoff

This design is intentionally aligned to the Azure MCP repository patterns: service-oriented toolsets, strict option validation, resource-scoped commands, and human-over-the-loop controls.

---

## Scope

The agent will support the following L1 scenarios:

- VNet, subnet, route table, NSG, and private endpoint validation
- App Service / Function / Container App health checks
- VM and OS diagnostics: event logs, process list, high CPU, memory pressure
- connectivity checks and DNS resolution validation
- deployment failure triage
- application log analysis for L2 support handoff
- ITSM ticket lifecycle updates and incident routing

This is not a free-form production admin bot. The agent is scoped to safe investigation, correlation, and controlled remediation with explicit approval gates.

---

## Design principles

### 1. Human-in-the-loop for all write operations

All write operations require explicit approval before execution.

Allowed write actions under approved workflows:
- create or update a non-production resource
- add or modify a temporary diagnostic setting
- restart a non-critical environment after approval
- update DNS or NSG only in a restricted, validated workflow

Blocked without approval:
- deleting resources
- modifying production network policy
- redeploying critical apps
- changing production DNS
- restarting production workloads automatically

### 2. Read-first, act-second

The agent should always collect evidence before taking action. This includes:
- Azure resource health
- VM or app metrics
- event logs
- deployment history
- recent config drift
- connectivity and DNS checks

### 3. Correlate symptoms across layers

A single incident often spans multiple layers. The agent should correlate:
- Azure infra state
- OS state
- app health
- deployment changes
- network configuration
- ITSM incident context

### 4. Safe default for incidents

When confidence is low, the agent should escalate rather than make a risky change.

### 5. Tooling must remain composable

The repo pattern favors small, clear commands grouped by resource and service. That model should remain the basis for L1 operations.

---

## Core agent model

### L1 operations agent

Responsibilities:
- read ITSM ticket details
- identify impacted environment and Azure resources
- run safe diagnostics
- classify the issue category
- execute approved safe actions
- update the ticket with evidence
- escalate to L2 when required

### L2 application support layer

Responsibilities:
- deep application log analysis
- deployment rollback investigation
- runtime exception review
- configuration drift comparison
- code or dependency issue validation

### ITSM integration layer

Responsibilities:
- fetch incident details
- update ticket status and comments
- store findings and action history
- route issues to correct support queue

---

## Target architecture

### 1. Azure MCP toolsets

The MCP server should expose operational toolsets grouped by resource area:

- network
  - VNet list/create/update
  - subnet management
  - NSG and route table updates
  - VNet peering
  - private endpoint lifecycle
  - private DNS zone and link management

- compute
  - VM status and health
  - VM sizing and quota checks
  - VM extension diagnostics
  - process and performance checks

- appservice
  - App Service plan create/update
  - web app health checks
  - VNet integration configuration
  - deployment and slot status

- app platform
  - Function App checks
  - Container App health and revision diagnostics
  - Azure Container Apps networking validation

- observability
  - Azure Monitor query support
  - Log Analytics query support
  - App Insights query support
  - alerts and resource health status

- deployment
  - deployment status checks
  - release pipeline logs review
  - config drift and deployment version checks

- identity and access
  - managed identity checks
  - RBAC validation
  - service principal permission verification

### 2. ITSM adapter

A dedicated integration service should provide:
- incident read endpoints
- change ticket lookup
- comment creation
- status updates
- priority and ownership mapping
- resumable workflow state

### 3. Remote execution adapter for OS diagnostics

For VM troubleshooting, use approved execution channels only:
- Azure VM Run Command for Windows/Linux diagnostics
- Azure Monitor Agent and log collection where appropriate
- secure access limited to approved machines and approved commands

The agent must never execute arbitrary shell commands on production hosts without explicit approval and policy controls.

### 4. Correlation engine

This layer normalizes findings into a common incident timeline:
- timestamp
- classification
- impacted resource
- evidence source
- confidence level
- recommended next action

This is what allows L1 to route incidents accurately.

---

## Incident workflow

### Workflow 1: connectivity incident

1. Read ITSM ticket and affected service.
2. Query Azure resource topology.
3. Check VNet, subnet, NSG, route table, private endpoint, and DNS records.
4. Evaluate whether routing or IP reachability is misconfigured.
5. If network is healthy, check OS-level connectivity and event logs.
6. Add findings to ITSM ticket.
7. Escalate to L2 if application or runtime issue is suspected.

### Workflow 2: high CPU or memory issue

1. Check Azure VM or App Service metrics.
2. Pull process list and top consumers.
3. Review recent deployment or configuration changes.
4. Check event logs and application logs.
5. Determine whether issue is infrastructure, application, or code-related.
6. If app-specific, open L2 handoff with timeline and evidence.
7. If platform issue, perform approved remediation or gather approval if required.

### Workflow 3: deployment failure

1. Read deployment status and pipeline logs.
2. Check health of the target resource group and app host.
3. Compare deployment version and config drift.
4. Inspect startup/runtime logs for errors.
5. Correlate with DNS, VNet, and private endpoint state.
6. Route to L2 or perform controlled rollback after approval.

---

## Human approval model

### Approval gates

The approval model should be enforced by workflow stage:

- Stage 1: read-only diagnostics: always allowed
- Stage 2: safe write actions in non-production: allowed with approval
- Stage 3: production network change: requires explicit human confirmation
- Stage 4: restart or deployment actions: requires verified approval and rollback plan
- Stage 5: destructive changes: blocked by default

### Approval metadata

Each attempted write action should capture:
- ticket ID
- reason
- impacted resource
- requested change
- planned rollback
- approver
- timestamp

---

## Safety and risk controls

### Required controls

- dry-run validation before write operations
- CIDR overlap verification before VNet or subnet changes
- route/NSG conflict review before applying network changes
- DNS record validation before updates
- dependency ordering for network and app changes
- service health verification after each change
- rollback strategy for each write action

### Disallowed behavior

- automatic changes to production networking without approval
- unbounded file or script execution on hosts
- broad log access outside ticket scope
- modifying unrelated Azure resources
- deleting resources without explicit approval and a clear rollback plan

---

## Proposed tool families

The MCP server can expose the following operational tool families:

### A. resource discovery
- list resources by type
- find impacted service by tag or naming convention
- check resource health and regions

### B. network diagnostics
- network topology summary
- NSG rule review
- route table summary
- private endpoint state
- private DNS zone state
- DNS record validation

### C. VM diagnostics
- VM status and health
- process list
- CPU and memory status
- service status
- event log extraction
- recent startup and error events

### D. app diagnostics
- App Service health
- Function App status
- Container App health and revision status
- application log retrieval
- deployment history and status

### E. incident support workflow
- fetch ticket context
- add evidence to ticket
- classify issue category
- route to support tier

---

## Example L1 decision tree

### Scenario: application cannot connect to private endpoint

1. Check service health
2. Check private endpoint state
3. Check VNet peering
4. Check NSG and route tables
5. Check private DNS zone records
6. Check VM or app host for DNS resolution failure
7. If DNS mismatch is found, escalate with evidence or perform approved fix
8. Record root cause and update ITSM ticket

### Scenario: VM shows high CPU and memory growth

1. Check Azure metrics
2. Pull process list and memory usage
3. Check recent deployment or application restart
4. Review Windows event logs or Linux journal entries
5. If issue is caused by app process, escalate to L2
6. If system-level issue, perform safe restart after approval

### Scenario: deployment failed after config change

1. Check deployment pipeline status
2. Compare current config to last successful version
3. Inspect app startup and HTTP logs
4. Check if VNet or DNS changes caused the breakage
5. Route the issue to the correct queue and document evidence

---

## Implementation phases

### Phase 1: read-only triage foundation

Implement the safest initial capabilities:
- resource inventory
- health checks
- Azure Monitor query support
- VNet/subnet and DNS inspection
- app health summary
- ticket evidence capture

This phase should be fully safe and no-risk.

### Phase 2: human-approved write workflows

Add controlled write tools for:
- non-production resource creation
- diagnostic configuration
- temporary NSG and route adjustments with approval
- private DNS zone updates under explicit validation
- app service VNet integration changes with approval

### Phase 3: advanced correlation and routing

Add integrated troubleshooting workflows:
- cross-layer timeline builder
- runtime log clustering
- issue classification by service/failure pattern
- L2 escalation pack generation

### Phase 4: ITSM automation and closure

Add:
- incident enrichment
- status updates and actions recorded in ticket system
- final closure or escalation recommendations
- audit trail of all commands and approvals

---

## Acceptance criteria

The design is successful when the following are true:

- all write operations require human approval
- L1 diagnostics can classify common Azure and app incidents from evidence
- incidents are correlated across Azure, OS, and app layers
- ITSM tickets are updated with structured findings
- L2 escalation includes relevant logs, metrics, and timeline
- network changes are validated before execution
- rollback plans exist for all riskier write actions

---

## Recommended next steps for this repo

1. Add a design document under docs/design
2. Define the first toolset boundaries:
   - network
   - compute
   - appservice
   - observability
   - incident workflow
3. Implement read-only L1 command skeletons first
4. Add approval-gate abstractions before any write operations are exposed
5. Build the ITSM integration adapter next
6. Validate with representative failure scenarios and recorded tests

This creates a safe and realistic starting point for the Azure L1 support agent without overexposing production-changing capabilities.

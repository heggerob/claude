# Mac Guardian — Product Plan

> **The product promise:** Start work from anywhere. Mac Guardian keeps the Mac, agents and development process running until the task is truly finished.

## Table of contents

1. [Core idea](#1-core-idea)
2. [Product parts](#2-product-parts)
3. [Project system](#3-project-system)
4. [Session system](#4-session-system)
5. [Start work from anywhere](#5-start-work-from-anywhere)
6. [Guardian workflow engine](#6-guardian-workflow-engine)
7. [Xcode Cache Doctor](#7-xcode-cache-doctor)
8. [Mac health and recovery](#8-mac-health-and-recovery)
9. [Proof of completion](#9-proof-of-completion)
10. [Prompt preparation](#10-prompt-preparation)
11. [Model and agent router](#11-model-and-agent-router)
12. [Usage limits and queued work](#12-usage-limits-and-queued-work)
13. [Plugin and MCP manager](#13-plugin-and-mcp-manager)
14. [Further strong feature ideas](#14-further-strong-feature-ideas)
15. [Security model](#15-security-model)
16. [Technical structure](#16-technical-structure)
17. [Development roadmap](#17-development-roadmap)
18. [Business model](#18-business-model)
19. [What makes it valuable](#19-what-makes-it-valuable)

---

## 1. Core idea

Mac Guardian is a control centre that:

- Keeps your development Mac ready
- Starts AI coding sessions remotely
- Watches agents, builds and system health
- Recovers crashed or stuck work
- Runs complete development workflows
- Connects Claude, Codex, Cursor and future agents
- Lets you control everything from iPhone

Mac Guardian is not another AI model, IDE or basic remote terminal. It is the supervisor around the whole development computer.

---

## 2. Product parts

### Mac Guardian Core

A background service running on the Mac.

It:

- Starts automatically after login
- Keeps selected work active
- Watches agent processes
- Runs commands
- Monitors Xcode builds
- Stores session states
- Executes automation rules
- Communicates securely with the iPhone app
- Restores work after crashes or restarts

### Mac Guardian for macOS

The visual Mac application.

Main sections:

1. Overview
2. Projects
3. Sessions
4. Workflows
5. Xcode and Storage
6. Activity
7. Security and Permissions

### Mac Guardian for iPhone

The remote control.

Main tabs:

1. Home
2. Projects
3. Inbox
4. Sessions
5. Settings

The **Inbox** is especially important. It gathers everything requiring attention:

- Agent needs permission
- Build failed
- Agent appears stuck
- Usage limit reached
- Test failed
- Workflow completed
- Mac went offline

---

## 3. Project system

The user adds development projects to Guardian.

Each project stores:

- Project name
- Folder location
- Git repository
- Xcode project or workspace
- Default scheme
- Default simulator or device
- Build command
- Test command
- Agent preferences
- Required MCP servers
- Skills and instruction files
- Environment setup
- Cleanup rules
- Completion requirements

### Example: SangBoken

- Workspace: `SangBoken.xcworkspace`
- Scheme: `SangBoken`
- Device: iPhone 16 Pro simulator
- Main agent: Claude
- Reviewer: Codex
- Build after every completed task
- Run tests after important changes
- Never delete archives
- Notify Robin when build succeeds

Guardian can use `xcodebuild` to list schemes and perform builds, tests, analysis and archives from the command line. *(Apple Developer)*

---

## 4. Session system

Every session has a clear status:

- Ready
- Starting
- Working
- Running command
- Waiting for approval
- Waiting for user
- Rate limited
- Possibly stuck
- Building
- Testing
- Failed
- Completed
- Recovering
- Offline

Each session shows:

- Agent and model
- Project
- Current task
- Current activity
- Files changed
- Commands running
- Time running
- Estimated cost
- Last meaningful progress
- Build and test state

The session should not only show raw terminal text. Guardian should convert the activity into simple events:

> Claude is editing CalendarView.swift.
>
> Codex started the test suite.
>
> Build failed with three Swift errors.
>
> No files or commands have changed for 15 minutes.

---

## 5. Start work from anywhere

From the iPhone:

1. Choose a Mac
2. Choose a project
3. Choose an agent
4. Write or speak the task
5. Choose a workflow
6. Start

Example:

> Add song favourites to SangBoken. Use the existing design system. Build and test the app when finished.

Guardian then:

1. Checks that the Mac is ready
2. Checks available storage and memory pressure
3. Opens the correct project folder
4. Prepares the agent instructions
5. Starts the session
6. Watches the work
7. Builds the app
8. Sends failures back to the agent
9. Notifies you when it has evidence that the task works

No manual `/rc` should be needed for sessions started by Guardian.

For Claude, Guardian could use either Claude's Agent SDK or managed CLI sessions with lifecycle hooks. Claude hooks can send structured events through shell commands or HTTP when session events occur. *(Claude Platform Docs)*

For Codex, Guardian can use the official App Server or SDK to start and resume threads, steer active work, execute commands, interrupt tasks and launch reviews. *(OpenAI Developers)*

---

## 6. Guardian workflow engine

A workflow is a sequence of controlled steps.

### Implement Feature

1. Understand request
2. Inspect project
3. Create plan
4. Make changes
5. Build
6. Fix build errors
7. Run tests
8. Review diff
9. Summarize work
10. Notify user

### Fix Build

1. Run build
2. Read exact errors
3. Send errors to agent
4. Build again
5. Repeat up to a set limit
6. Detect repeated errors
7. Suggest targeted cache cleanup
8. Ask user if the issue remains

### Safe Night Shift

1. Create a Git branch
2. Save current project state
3. Start agent
4. Allow only approved commands
5. Build after changes
6. Stop after repeated failures
7. Never merge automatically
8. Send morning summary

### Claude–Codex Handoff

1. Claude studies the problem and makes the plan
2. Codex implements it
3. Claude reviews the resulting code
4. Codex fixes confirmed issues
5. Guardian builds and tests
6. User receives one combined summary

### App Release

1. Check version and build number
2. Build release configuration
3. Run tests
4. Check signing
5. Create archive
6. Save logs
7. Ask for approval
8. Continue with validation or upload

---

## 7. Xcode Cache Doctor

This should be intelligent, not a large "Delete Everything" button.

Apple states that clean builds take significantly longer and should only be used when necessary. Xcode 26 also introduced compilation caching to improve performance after clean builds and branch changes. *(Apple Developer)*

### Cache Doctor levels

#### Level 1: Diagnose

Guardian checks:

- Available storage
- Current project's DerivedData size
- Build log
- Repeated compiler errors
- Stale build products
- Active Xcode processes
- Current Xcode version
- Selected command-line Xcode version
- Simulator condition

Nothing is deleted.

#### Level 2: Clean current build

- Run Xcode's normal clean action
- Remove only current build products
- Rebuild
- Compare the result

#### Level 3: Clean project DerivedData

- Stop active builds
- Show exactly what will be removed
- Delete only DerivedData belonging to the selected project
- Rebuild the project
- Record whether this solved the problem

#### Level 4: Developer storage cleanup

Show separate categories:

- DerivedData
- Swift package caches
- Module caches
- Old simulator data
- Old device support
- Build logs
- Old archives

Guardian must treat them differently:

- DerivedData: removable, but rebuilding costs time
- Simulator data: remove only selected unused simulators
- Archives: never automatically delete
- Project files: never touched
- Signing information: never touched

### Smart cleanup rules

Examples:

- If storage falls below 10 GB, notify the user
- If storage falls below 5 GB, pause new builds
- Suggest old project caches first
- Never clean a project currently building
- Never clear all caches because one build failed
- Keep caches used during the last seven days
- Show expected reclaimed storage before deletion
- Record everything that was removed

### Cache learning

Guardian records whether cleanup actually fixed the build.

Over time it can learn:

> This error was fixed by cleaning project DerivedData twice before.

Or:

> Cache cleanup has never fixed this error. The issue is probably in the code.

---

## 8. Mac health and recovery

Guardian watches:

- Memory pressure
- CPU usage
- Disk space
- Thermal state
- Network connection
- Xcode
- Simulator processes
- Claude, Codex and other agents
- Background builds
- Crashed processes

### Memory response

Instead of pretending to "free RAM," Guardian uses policies:

1. Detect critical memory pressure
2. Find the largest relevant processes
3. Pause starting new agents
4. Stop abandoned simulators
5. Offer to close inactive development applications
6. Restart a frozen agent if safe
7. Preserve its session information
8. Resume work after the system stabilizes

### Restart recovery

After the Mac restarts:

1. Guardian starts automatically
2. Checks unfinished sessions
3. Checks project and Git state
4. Checks whether commands were interrupted
5. Restores safe sessions
6. Does not repeat uncertain destructive commands
7. Notifies the user

### Stuck detection

A session may be stuck when:

- The same error repeats several times
- The same command runs repeatedly
- No files, output or task state changes
- The agent claims completion without a successful build
- An approval request has waited too long
- A background command never exits

Guardian can then:

- Ask the agent for a status explanation
- Interrupt the current action
- Retry once
- Change strategy
- Switch reviewer
- Notify the user
- Stop the workflow before wasting more tokens

---

## 9. Proof of completion

This could become one of Mac Guardian's strongest features.

An agent saying "done" does not mean the task is finished.

Each project defines completion rules:

- Project builds successfully
- Required tests pass
- No new compiler errors
- No uncommitted accidental files
- App launches
- Requested feature exists
- Review completed
- Screenshot captured if visual work changed

Guardian displays:

> **Agent says**
>
> Task completed.
>
> **Guardian verified**
>
> - Build: Passed
> - Tests: 14/14 passed
> - App launch: Passed
> - Code review: Two warnings
> - Visual verification: Not performed

The session is only marked **Verified Complete** after the required evidence passes.

---

## 10. Prompt preparation

Before sending a rough prompt, Guardian can use a cheaper model to improve it.

Original:

> Make the calendar better and fix bugs.

Prepared:

> Inspect the calendar screen and identify reproducible problems. Fix layout and interaction bugs without redesigning unrelated screens. Follow the existing design system. Build the project and report each change.

The user can choose:

- Send original
- Use improved prompt
- Review changes first
- Never alter my prompts

Prompt preparation can add:

- Project context
- Relevant files
- Acceptance requirements
- Build requirements
- Safety limits
- Design-system rules
- Previous failed attempts
- Definition of done

The cheaper model should not secretly change the user's meaning.

---

## 11. Model and agent router

Guardian chooses based on rules first and AI judgment second.

Examples:

- Small text edit → cheapest fast model
- Large architectural change → stronger reasoning model
- SwiftUI design work → preferred visual/UI agent
- Code review → separate reviewer
- Repeated failure → switch agent or model
- Sensitive task → local-only agent
- Low budget remaining → queue non-urgent work

The user sets:

- Preferred providers
- Maximum cost per task
- Models never to use
- Tasks requiring approval
- Whether provider switching is allowed
- Whether prompts may leave the computer

Guardian should show why it selected something:

> Chose Codex because this workflow requires structured thread control and automated review.

> Chose the cheaper model because this is a small documentation change.

---

## 12. Usage limits and queued work

When an agent reaches a genuine provider limit:

- Mark session as Rate Limited
- Save the exact task state
- Keep the project protected
- Estimate when it may be available again only when reliable information exists
- Queue a preset continuation message
- Resume after availability returns
- Notify the user before resuming when required

Example continuation:

> Continue from the existing task state. First inspect the changes already made, then complete the remaining work and run the required build.

Guardian must not attempt to bypass provider limits. It simply preserves and resumes allowed work.

---

## 13. Plugin and MCP manager

Guardian should not automatically install random MCP servers.

Instead, it provides a curated system.

Each integration shows:

- Name
- Developer
- Purpose
- Commands or tools exposed
- Files it can access
- Network access
- Required secrets
- Projects using it
- Last health check
- Verified or unverified status

### Plugin packs

**iOS Development Pack**

- Xcode build
- Simulator launch
- Test runner
- Screenshot capture
- Build-log reader
- App Store checklist

**Web Development Pack**

- Development server
- Browser testing
- Build
- Lint
- Deployment logs

**GitHub Pack**

- Issues
- Branches
- Pull requests
- Reviews
- CI checks

Guardian can automatically prepare recommended integrations, but installation and permissions should remain visible.

Claude Code and Codex both support MCP connections, making a shared Guardian MCP layer practical. *(OpenAI Developers)*

---

## 14. Further strong feature ideas

### Guardian Replay

Reconstruct what happened before a crash:

- Prompt
- Agent
- Files changed
- Commands
- Build state
- Last successful checkpoint
- Reason for stopping

### Loop Guard

Detects when the agent is wasting time repeating the same attempted fix.

### Project Time Machine

Before autonomous work:

- Create Git checkpoint
- Record uncommitted changes
- Save task instructions
- Allow one-tap rollback

### Morning Report

Shows:

- Tasks attempted
- Tasks completed
- Files changed
- Builds and tests
- Failed attempts
- Money used
- Decisions requiring attention

### Remote Bug Drop

From your phone:

1. Select project
2. Add screenshot or screen recording
3. Speak the bug
4. Guardian creates the task
5. Agent investigates on the Mac

### Environment Doctor

Checks:

- Xcode command-line selection
- Xcode license state
- Required runtimes
- Simulator availability
- Package dependencies
- Git condition
- Storage
- Agent installation
- MCP health

### Agent Referee

When two agents disagree, Guardian shows:

- Each proposed solution
- Evidence
- Risks
- Build results
- Reviewer conclusion

### Emergency Stop

One button immediately:

- Stops new commands
- Interrupts active agents
- Preserves logs
- Leaves project files intact
- Marks sessions for review

### Focus Mode

Temporarily gives one important agent priority while pausing less important builds and agents.

### Work Queue

Create tasks from your phone throughout the day. Guardian completes them in order based on:

- Priority
- Available model
- Cost
- Project conflicts
- Mac resources
- Required user approval

---

## 15. Security model

Mac Guardian controls terminal commands and project files, so security must be central.

Required protections:

- QR pairing between Mac and iPhone
- End-to-end encrypted remote messages
- Provider credentials stored in macOS Keychain
- Guardian servers never receive provider passwords
- Face ID for dangerous remote approvals
- Command allowlists
- Detailed activity log
- Git checkpoint before autonomous changes
- Separate permissions per project
- Immediate session shutdown
- Automatic expiration of remote device access

Dangerous actions require stronger approval:

- Deleting files outside build caches
- Force-pushing Git
- Changing signing certificates
- Uploading releases
- Installing software
- Running administrator commands
- Accessing unrelated folders

Because Mac Guardian needs broad project, process and command-line control, the Mac application would initially be distributed directly with Developer ID signing and Apple notarization instead of designing around Mac App Store sandbox restrictions. Apple requires sandboxing for Mac App Store apps, while Developer ID and notarization support direct Mac distribution. *(Apple Developer)*

---

## 16. Technical structure

### Guardian Core

Native Swift background service responsible for:

- Machine health
- Process supervision
- Workflow execution
- Project access
- Storage inspection
- Secure remote connection
- Local database
- Notifications

### Guardian Agent Bridge

Separate adapter for each system:

- ClaudeAdapter
- CodexAdapter
- CursorAdapter
- GenericTerminalAdapter

Every adapter implements:

- Start
- Resume
- Send message
- Interrupt
- Read state
- Read output
- Detect waiting
- Detect failure
- Get usage
- Stop

The generic terminal adapter is important. It prevents Guardian from depending entirely on individual companies.

### Guardian Build Engine

Responsible for:

- Detecting Xcode projects
- Discovering schemes
- Choosing destination
- Running `xcodebuild`
- Reading build results
- Running tests
- Launching Simulator
- Managing targeted cleanup

### Guardian Workflow Engine

Uses:

- Trigger
- Conditions
- Steps
- Retry limits
- Approval gates
- Timeout
- Completion requirements
- Recovery action

### Guardian Relay

The Mac makes an outbound encrypted connection to the relay. The user should not need to expose ports or configure their router.

---

## 17. Development roadmap

### Phase 0 — Personal proof of concept

**Goal:** Control one SangBoken coding task from the iPhone and receive a verified build result.

Build:

- Small Mac background service
- Add project folders
- Start generic terminal command
- Start Claude or Codex
- Stream output
- Send follow-up message
- Run `xcodebuild`
- Send notification
- Detect crashed process

No polished design yet.

### Phase 1 — Guardian Local

- Mac menu bar app
- Projects
- Session process manager
- Keep-awake mode
- Resource monitoring
- Start after login
- Restart recovery
- Build and test actions
- Targeted DerivedData cleanup
- Activity history

### Phase 2 — Guardian Remote

- Secure device pairing
- iPhone dashboard
- Start sessions remotely
- Session Inbox
- Push notifications
- Remote approvals
- Build controls
- Emergency stop
- Attach screenshots and prompts

### Phase 3 — Agent integration

- Proper Codex App Server integration
- Claude SDK or hook integration
- Generic agent adapter
- Session statuses
- Resumable tasks
- Cost and usage information
- Provider-limit queue

### Phase 4 — Automation

- Workflow builder
- Retry rules
- Completion verification
- Build-failure feedback
- Night Shift
- Checkpoints and rollback
- Loop Guard
- Morning Report

### Phase 5 — Multi-agent system

- Planner, builder, reviewer and tester roles
- Agent handoffs
- Parallel worktrees
- Model router
- Prompt preparation
- Plugin packs
- Shared project memory

### Phase 6 — Public product

- Setup assistant
- Automatic project detection
- Direct-download installer
- Notarization
- Subscription and account system
- Documentation
- Privacy controls
- Crash reporting
- Beta testing

---

## 18. Business model

### Free

- One Mac
- Two projects
- Local monitoring
- Basic Xcode Cache Doctor
- Manual builds
- One active agent

### Guardian Pro

- iPhone remote control
- Unlimited projects
- Multiple agents
- Automation workflows
- Recovery rules
- Notifications
- Verified completion
- Model routing
- Plugin packs

Possible price:

- Approximately €4–6 monthly
- Approximately €40–50 yearly
- No additional percentage on AI usage

### Guardian Local Lifetime

For users who do not need remote relay:

- One-time payment
- Local monitoring
- Cache tools
- Process supervision
- Local workflows

This gives users a non-subscription choice while remote infrastructure can remain subscription-based.

---

## 19. What makes it valuable

Mac Guardian should win through this exact combination:

1. Cross-agent control
2. Local Mac reliability
3. Xcode-aware building and recovery
4. Proof that work actually succeeded
5. Safe unattended workflows
6. Simple iPhone control
7. Automatic recovery after crashes, limits and restarts

Remote chat alone will be copied or built directly into every provider. Claude already offers local Remote Control, while Codex offers its own multi-agent desktop and programmable App Server. *(OpenAI Developers)*

The defensible product is:

> **A provider-independent supervisor that keeps the whole development machine and workflow working correctly.**

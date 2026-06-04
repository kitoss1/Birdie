# CLAUDE.md
This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
This file provides technical guidance for working with the Birdie codebase. For the moment is a bit barebones because the project is a WIP, it will be more complete in the future.

## Key Development Commands

### Building
- Manual builds can be performed through Unity Editor.

### Code Quality
- Coding convention guidelines can be found in 'docs/base/coding-style.md' and '.editorconfig', important to follow them closely

## Architecture Overview

### Core Game Structure
- **Main Scripts**: Located in `Assets/Scripts/` organized into:
   - 'Managers' - Main managers for the game

### Third-Party Dependencies
- **DOTween**: Animation framework
- **UniTask**: Async/await support for Unity

### Planning and Documentation
- After providing a plan how to implement requested feature/bugfix, verify from local Unity docs using unity-docs agent that the methods you are available and are not deprecated

## Development Workflow
- Follow the coding conventions and style guidelines in .editorconfig and docs/base/coding-style.md **IMPORTANT**
- Code reviews required from developers
- When creating new files, don't create .meta files manually, Unity will generate them automatically. However, ensure that .meta files are included in version control for all assets and scripts

## Task Management and TODO Lists
- When working with multi-step operations, complex tasks, or running processes, always use TODO lists to track progress and provide transparency:

### When to Use TODO Lists
- **Multi-step tasks**: Any operation requiring 3 or more distinct steps
- **Testing workflows**: Running tests, fixing failing tests, verifying fixes
- **Feature implementation**: Breaking down features into manageable tasks
- **Debugging sessions**: Tracking investigation steps and fixes
- **Build and deployment**: Managing compilation, testing, and deployment steps
- **Code reviews and refactoring**: Systematic code improvements

### Best Practices
- **Proactive creation**: Create TODO lists before starting complex work
- **Real-time updates**: Mark tasks as in_progress before starting, completed immediately after finishing
- **Task breakdown**: Break complex tasks into smaller, actionable steps
- **One active task**: Only one task should be in_progress at any time
- **Clear descriptions**: Use imperative form for task content (e.g., "Run unit tests", "Fix authentication bug")
- **Active form tracking**: Provide present continuous form for in-progress display (e.g., "Running unit tests", "Fixing authentication bug")

### Integration with Development Workflow
- Track progress when implementing features that span multiple files or components
- Organize testing workflows, especially when fixing multiple failing tests
- Coordinate with git workflow when preparing commits or pull requests
- Manage dependencies between tasks in complex feature implementations

### Technical Documentation
- **docs/base/coding-style.md** - C# coding standards, .editorconfig support, analyzer rules

### Product Documentation
- **docs/product/big-picture.md** - 

### Feature Development Context
- When implementing new features, first review `docs/product/big-picture.md` to understand game context and target audience
- The product documentation provides essential context for making design decisions that align with game goals
- Always run task-creator agent to create tasks when refactor-planner runs to have plan what to implement on the refactoring

## ***IMPORTANT*** Notes
- Check that the imports have the proper namespaces and that there are no unused imports
- Check assembly definitions if new scripts are added to the project
- Check that used code is ont accessible from the assembly definition of the script
- Use DebugBase class for logging
- Don’t hardcode class names in logs. Instead use nameof(ClassName) to ensure logs remain accurate during refactoring
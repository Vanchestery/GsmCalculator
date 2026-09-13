# Contributing
This is a personal portfolio project, but PRs and issues are welcome.
## Environment
- **.NET 9 SDK** ([download](https://dotnet.microsoft.com/download))
- **Windows 10/11** (WPF is Windows-only)
- **Visual Studio 2022** 17.12+ or **JetBrains Rider** 2024.3+
```bash
git clone https://github.com/Vanchestery/GsmCalculator.git
cd GsmCalculator
dotnet restore
dotnet build
dotnet test
```
## Code style
- Follow the existing style (`Nullable enable`, file-scoped namespaces, `var` for obvious types).
- Public API should have XML documentation comments.
- Inline code comments stay in **Russian** (same as the existing codebase).
## Architecture
- **MVVM**: Views must not know about Services; ViewModels must not open other windows directly (use `I*WindowService`).
- **Services** are behind interfaces for testability.
- **Models** are plain POCOs without `INotifyPropertyChanged`.
## Tests
New business logic should come with tests in `GsmCalculator.Tests/`.
- Pure services — no Moq, direct tests.
- File-based services — use temp files via `Path.GetTempPath()`.
- ViewModels — real pure services + Moq for side-effect dependencies.
## Commit format
Use [Conventional Commits](https://www.conventionalcommits.org/):
```
type(scope): short subject
Optional longer body explaining what and why.
```
Types: `feat`, `fix`, `chore`, `docs`, `ci`, `test`, `refactor`, `perf`, `style`.
## Pull Requests
1. Fork the repo and create a feature branch from `main`.
2. Make sure `dotnet build` and `dotnet test` pass.
3. Describe the change in the PR — what and why.

# Contributing

Это персональный проект-портфолио, но PR и issue приветствуются.

## Окружение

- **.NET 9 SDK** ([скачать](https://dotnet.microsoft.com/download))
- **Windows 10/11** (WPF — windows-only)
- **Visual Studio 2022** 17.12+ или **JetBrains Rider** 2024.3+

```bash
git clone https://github.com/USERNAME/GsmCalculator.git
cd GsmCalculator
dotnet restore
dotnet build
dotnet test
```

## Стиль кода

- Соблюдайте существующий стиль (`Nullable enable`, file-scoped namespaces,
  `var` для очевидных типов).
- Public API должны быть документированы XML-комментариями.
- Комментарии в коде — на русском (как в существующем коде).

## Архитектурные принципы

- **MVVM**: View не должен знать про Services; ViewModel не должен напрямую
  открывать другие окна (используйте `I*WindowService`).
- **Services** изолированы за интерфейсами для тестируемости.
- **Модели** — чистые POCO без `INotifyPropertyChanged`.

## Тесты

Любая новая бизнес-логика должна сопровождаться тестами в `GsmCalculator.Tests/`.

- Чистые сервисы — без Moq, прямые тесты.
- Файловые сервисы — на временных файлах через `Path.GetTempPath()`.
- ViewModels — с реальными чистыми сервисами + Moq для зависимостей с побочными эффектами.

## Формат коммитов

Используется [Conventional Commits](https://www.conventionalcommits.org/ru/):

```
type(scope): short subject

Optional longer body explaining what and why.
```

Типы: `feat`, `fix`, `chore`, `docs`, `ci`, `test`, `refactor`, `perf`, `style`.

## Pull Requests

1. Форкните репозиторий и создайте feature-ветку от `main`.
2. Убедитесь что `dotnet build` и `dotnet test` проходят.
3. Опишите изменения в PR — что и зачем.

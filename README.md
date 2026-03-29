# 🍴 RecipeApp — WPF Приложение для рецептов

## Описание
Современное WPF-приложение для ведения личной кулинарной книги. Позволяет добавлять, просматривать и удалять рецепты с ингредиентами, шагами и фотографиями.

## Функции
- 📋 Список рецептов с поиском и фильтрацией по категориям
- ✏️ Форма добавления / редактирования с:
  - Названием, описанием, категорией, сложностью
  - Временем подготовки и готовки, количеством порций
  - Динамическим списком ингредиентов
  - Пронумерованными шагами приготовления
  - Фото блюда
- 🗄️ Хранение данных в SQLite (локально, без сервера)
- 🎨 Современный UI с тёплой цветовой схемой

## Технологии
| Библиотека | Версия | Назначение |
|---|---|---|
| .NET 8 WPF | 8.0 | Платформа |
| CommunityToolkit.Mvvm | 8.3 | MVVM паттерн |
| EF Core + SQLite | 8.0 | База данных |

## Запуск

### Требования
- .NET 8 SDK (https://dotnet.microsoft.com/download)
- Windows 10/11

### Команды
```bash
# Восстановить зависимости
dotnet restore

# Запустить приложение
dotnet run

# Собрать релизную версию
dotnet publish -c Release -r win-x64 --self-contained
```

## Структура проекта
```
RecipeApp/
├── Models/
│   └── Recipe.cs           # Модели Recipe, Ingredient, Step
├── ViewModels/
│   ├── MainViewModel.cs     # Список рецептов
│   └── EditRecipeViewModel.cs # Форма редактирования
├── Views/
│   ├── MainWindow.xaml      # Главное окно
│   └── EditRecipeWindow.xaml # Диалог редактирования
├── Data/
│   └── RecipeDbContext.cs   # EF Core контекст
├── Services/
│   └── RecipeService.cs     # CRUD операции
├── Converters/
│   └── Converters.cs        # Value converters
└── Themes/
    ├── Colors.xaml           # Цветовая палитра
    └── Styles.xaml           # Стили контролов
```

## База данных
SQLite файл создаётся автоматически по пути:
`%LOCALAPPDATA%\RecipeApp\recipes.db`

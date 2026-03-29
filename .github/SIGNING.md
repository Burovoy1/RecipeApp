# Подпись APK для релиза

## Генерация keystore (один раз)

```bash
keytool -genkey -v \
  -keystore recipeapp.keystore \
  -alias recipeapp \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

## Добавление секретов в GitHub

Перейди в **Settings → Secrets and variables → Actions** и добавь:

| Secret            | Значение                                     |
|-------------------|----------------------------------------------|
| `KEYSTORE_BASE64` | `base64 -w 0 recipeapp.keystore`             |
| `KEYSTORE_ALIAS`  | Алиас, указанный при генерации (напр. `recipeapp`) |
| `KEYSTORE_PASS`   | Пароль хранилища                             |
| `KEY_PASS`        | Пароль ключа                                 |

### На Windows (PowerShell):
```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("recipeapp.keystore")) | clip
# Вставляет base64 в буфер обмена
```

## Как создать релиз с APK

1. Создай тег: `git tag v1.0.0 && git push origin v1.0.0`
2. На GitHub: **Releases → Draft a new release** → выбери тег → **Publish**
3. Workflow соберёт **подписанный APK** и прикрепит к релизу автоматически

## Debug APK (без ключа)

При каждом `push` в main автоматически собирается **debug APK** и сохраняется
в **Actions → выбери run → Artifacts → RecipeApp-debug-apk** (хранится 14 дней).

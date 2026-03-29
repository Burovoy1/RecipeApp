using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RecipeApp.Models;

namespace RecipeApp.Services;

public class ImportedRecipe
{
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int    PrepTime    { get; set; }
    public int    CookTime    { get; set; }
    public int    Servings    { get; set; }
    public List<ImportedIngredient> Ingredients { get; set; } = new();
    public List<string>             Steps       { get; set; } = new();
    public string? ImageUrl       { get; set; }
    public string? LocalImagePath { get; set; }
    // nutritionInfo из eda.ru (на всё блюдо)
    public double Calories    { get; set; }
    public double Protein     { get; set; }
    public double Fat         { get; set; }
    public double Carbs       { get; set; }
}

public class ImportedIngredient
{
    public string Name   { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Unit   { get; set; } = string.Empty;
}

public class RecipeImportService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36" },
            { "Accept-Language", "ru-RU,ru;q=0.9" },
        }
    };

    public async Task<ImportedRecipe> ImportFromUrlAsync(string url)
    {
        if (!url.StartsWith("http")) url = "https://" + url;

        var html = await _http.GetStringAsync(url);
        var doc  = new HtmlDocument();
        doc.LoadHtml(html);

        // 1. Пробуем __NEXT_DATA__ (Next.js — eda.ru)
        var recipe = TryParseNextData(doc)
        // 2. Пробуем Schema.org JSON-LD
                  ?? TryParseJsonLd(doc)
        // 3. Фолбэк — прямой HTML
                  ?? TryParseHtml(doc)
                  ?? throw new Exception(
                      "Не удалось распознать рецепт на этой странице.\nПопробуйте другую ссылку.");

        if (!string.IsNullOrEmpty(recipe.ImageUrl))
            recipe.LocalImagePath = await DownloadImageAsync(recipe.ImageUrl);

        return recipe;
    }

    // ── 1. __NEXT_DATA__ (Next.js) ───────────────────────────────────
    private ImportedRecipe? TryParseNextData(HtmlDocument doc)
    {
        try
        {
            var script = doc.DocumentNode
                .SelectSingleNode("//script[@id='__NEXT_DATA__']");
            if (script == null) return null;

            using var root = JsonDocument.Parse(script.InnerText);

            // Ищем объект рецепта в props -> pageProps -> recipe (или похожих путях)
            if (!root.RootElement.TryGetProperty("props", out var props)) return null;
            if (!props.TryGetProperty("pageProps", out var pageProps)) return null;

            // Eda.ru хранит рецепт в pageProps.recipe
            JsonElement recipeEl;
            if (pageProps.TryGetProperty("recipe", out recipeEl) ||
                pageProps.TryGetProperty("data", out recipeEl))
            {
                return ParseNextRecipeObject(recipeEl);
            }

            // Иногда вложено глубже
            foreach (var prop in pageProps.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var r = ParseNextRecipeObject(prop.Value);
                    if (r != null && !string.IsNullOrEmpty(r.Title)) return r;
                }
            }
        }
        catch { }
        return null;
    }

    private ImportedRecipe? ParseNextRecipeObject(JsonElement el)
    {
        var title = GetStr(el, "name") ?? GetStr(el, "title");
        if (string.IsNullOrEmpty(title)) return null;

        var r = new ImportedRecipe { Title = title };

        // Описание
        r.Description = CleanHtml(
            GetStr(el, "description") ??
            GetStr(el, "announcement") ??
            GetStr(el, "story") ??
            GetStr(el, "about") ?? "");

        // Время — eda.ru хранит как число минут
        r.PrepTime = ParseMinutes(el, "preparationTime", "prepTime", "prepTimeMinutes");
        r.CookTime = ParseMinutes(el, "cookingTime", "cookTime", "cookTimeMinutes", "totalTime");

        // Порции
        r.Servings = ParseServingsFromEl(el);

        // Фото — eda.ru: recipeCover.url или openGraphImageUrl
        r.ImageUrl = ParseEdaImageUrl(el) ?? ParseImageUrl(el);

        // ── Ингредиенты eda.ru: composition[].{ amount, measureUnit.nameTwo, ingredient.name }
        if (el.TryGetProperty("composition", out var composition) &&
            composition.ValueKind == JsonValueKind.Array)
        {
            foreach (var ing in composition.EnumerateArray())
                r.Ingredients.Add(ParseEdaIngredient(ing));
        }
        // Запасные ключи
        if (r.Ingredients.Count == 0)
        {
            foreach (var key in new[] { "ingredients", "recipeIngredient", "ingredientLines" })
            {
                if (!el.TryGetProperty(key, out var ings) ||
                    ings.ValueKind != JsonValueKind.Array) continue;
                foreach (var ing in ings.EnumerateArray())
                {
                    if (ing.ValueKind == JsonValueKind.String)
                        r.Ingredients.Add(ParseIngredientLine(ing.GetString() ?? ""));
                    else if (ing.ValueKind == JsonValueKind.Object)
                        r.Ingredients.Add(ParseIngredientObject(ing));
                }
                if (r.Ingredients.Count > 0) break;
            }
        }

        // ── Шаги eda.ru: recipeSteps[].description
        if (el.TryGetProperty("recipeSteps", out var recipeSteps) &&
            recipeSteps.ValueKind == JsonValueKind.Array)
        {
            foreach (var step in recipeSteps.EnumerateArray())
            {
                var text = GetStr(step, "description") ?? GetStr(step, "text") ?? "";
                if (!string.IsNullOrWhiteSpace(text))
                    r.Steps.Add(CleanHtml(text));
            }
        }
        // Запасные ключи
        if (r.Steps.Count == 0)
        {
            foreach (var key in new[] { "steps", "instructions", "recipeInstructions", "cookingSteps" })
            {
                if (!el.TryGetProperty(key, out var steps)) continue;
                r.Steps.AddRange(ParseStepsFromEl(steps));
                if (r.Steps.Count > 0) break;
            }
        }

        // nutritionInfo: { kilocalories, proteins, fats, carbohydrates }
        if (el.TryGetProperty("nutritionInfo", out var ni) &&
            ni.ValueKind == JsonValueKind.Object)
        {
            r.Calories = GetDouble(ni, "kilocalories");
            r.Protein  = GetDouble(ni, "proteins");
            r.Fat      = GetDouble(ni, "fats");
            r.Carbs    = GetDouble(ni, "carbohydrates");
        }

        return r;
    }

    // Парсинг ингредиента eda.ru:
    // { amount: 3, measureUnit: { nameTwo: "штуки" }, ingredient: { name: "Красный сладкий перец" } }
    private ImportedIngredient ParseEdaIngredient(JsonElement el)
    {
        var name   = "";
        var amount = "";
        var unit   = "";

        // Название
        if (el.TryGetProperty("ingredient", out var ingObj))
            name = GetStr(ingObj, "name") ?? GetStr(ingObj, "title") ?? "";

        // Количество
        if (el.TryGetProperty("amount", out var amtEl))
        {
            amount = amtEl.ValueKind == JsonValueKind.Number
                ? amtEl.ToString()
                : amtEl.GetString() ?? "";
        }

        // Единица — берём самую подходящую форму
        if (el.TryGetProperty("measureUnit", out var muEl) &&
            muEl.ValueKind == JsonValueKind.Object)
        {
            // nameTwo — форма для "2-4" (штуки, граммы)
            unit = GetStr(muEl, "nameTwo") ??
                   GetStr(muEl, "name") ??
                   GetStr(muEl, "nameFive") ?? "";
        }

        // Если название пустое — фолбэк через line
        if (string.IsNullOrEmpty(name))
        {
            var line = GetStr(el, "text") ?? GetStr(el, "title") ?? "";
            if (!string.IsNullOrEmpty(line))
                return ParseIngredientLine(line);
        }

        return new ImportedIngredient
        {
            Name   = CleanHtml(name),
            Amount = amount.Trim(),
            Unit   = unit.Trim()
        };
    }

    // Фото eda.ru: recipeCover или openGraphImageUrl
    private string? ParseEdaImageUrl(JsonElement el)
    {
        if (el.TryGetProperty("recipeCover", out var cover) &&
            cover.ValueKind == JsonValueKind.Object)
        {
            var url = GetStr(cover, "url") ?? GetStr(cover, "imageUrl");
            if (!string.IsNullOrEmpty(url)) return url;
        }
        return GetStr(el, "openGraphImageUrl");
    }

    private int ParseMinutes(JsonElement el, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!el.TryGetProperty(key, out var val)) continue;

            if (val.ValueKind == JsonValueKind.Number)
                return val.GetInt32();

            if (val.ValueKind == JsonValueKind.String)
            {
                var s = val.GetString() ?? "";
                // ISO duration PT1H30M
                if (s.StartsWith("PT") || s.StartsWith("P"))
                    return ParseIsoDuration(s);
                // Plain number string
                if (int.TryParse(s, out var n)) return n;
                var m = Regex.Match(s, @"\d+");
                if (m.Success) return int.Parse(m.Value);
            }
        }
        return 0;
    }

    private ImportedIngredient ParseIngredientObject(JsonElement el)
    {
        // eda.ru структура: { ingredient: { name }, amount: { value, unit } }
        var name = GetStr(el, "name") ?? "";

        if (string.IsNullOrEmpty(name) && el.TryGetProperty("ingredient", out var ingObj))
            name = GetStr(ingObj, "name") ?? GetStr(ingObj, "title") ?? "";

        var amount = "";
        var unit   = "";

        if (el.TryGetProperty("amount", out var amtEl))
        {
            if (amtEl.ValueKind == JsonValueKind.Object)
            {
                amount = GetStr(amtEl, "value") ?? GetStr(amtEl, "amount") ?? "";
                if (amtEl.TryGetProperty("unit", out var unitEl))
                {
                    unit = unitEl.ValueKind == JsonValueKind.Object
                        ? GetStr(unitEl, "title") ?? GetStr(unitEl, "name") ?? ""
                        : unitEl.GetString() ?? "";
                }
            }
            else if (amtEl.ValueKind == JsonValueKind.String)
                amount = amtEl.GetString() ?? "";
            else if (amtEl.ValueKind == JsonValueKind.Number)
                amount = amtEl.ToString();
        }

        // Запасные поля
        if (string.IsNullOrEmpty(amount))
            amount = GetStr(el, "quantity") ?? GetStr(el, "value") ?? "";
        if (string.IsNullOrEmpty(unit))
            unit = GetStr(el, "unit") ?? GetStr(el, "measure") ?? "";

        // Если название всё ещё пустое — пробуем line/text
        if (string.IsNullOrEmpty(name))
        {
            var line = GetStr(el, "text") ?? GetStr(el, "line") ?? GetStr(el, "title") ?? "";
            if (!string.IsNullOrEmpty(line))
                return ParseIngredientLine(line);
        }

        return new ImportedIngredient
        {
            Name   = CleanHtml(name),
            Amount = amount.Trim(),
            Unit   = unit.Trim()
        };
    }

    private List<string> ParseStepsFromEl(JsonElement stepsEl)
    {
        var result = new List<string>();
        if (stepsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in stepsEl.EnumerateArray())
            {
                if (s.ValueKind == JsonValueKind.String)
                {
                    var t = CleanHtml(s.GetString() ?? "");
                    if (!string.IsNullOrWhiteSpace(t)) result.Add(t);
                }
                else if (s.ValueKind == JsonValueKind.Object)
                {
                    // HowToStep или { text, description, step }
                    var text = GetStr(s, "text") ?? GetStr(s, "description")
                            ?? GetStr(s, "step") ?? GetStr(s, "instructions") ?? "";
                    if (!string.IsNullOrWhiteSpace(text))
                        result.Add(CleanHtml(text));

                    // Вложенные itemListElement
                    if (s.TryGetProperty("itemListElement", out var inner))
                        result.AddRange(ParseStepsFromEl(inner));
                }
            }
        }
        else if (stepsEl.ValueKind == JsonValueKind.String)
        {
            var t = CleanHtml(stepsEl.GetString() ?? "");
            if (!string.IsNullOrWhiteSpace(t)) result.Add(t);
        }
        return result;
    }

    // ── 2. Schema.org JSON-LD ────────────────────────────────────────
    private ImportedRecipe? TryParseJsonLd(HtmlDocument doc)
    {
        var scripts = doc.DocumentNode
            .SelectNodes("//script[@type='application/ld+json']");
        if (scripts == null) return null;

        foreach (var script in scripts)
        {
            try
            {
                using var root = JsonDocument.Parse(script.InnerText.Trim());
                var recipeEl   = FindRecipeElement(root.RootElement);
                if (recipeEl is null) continue;

                var r = new ImportedRecipe
                {
                    Title       = GetStr(recipeEl.Value, "name") ?? "",
                    Description = CleanHtml(GetStr(recipeEl.Value, "description") ?? ""),
                    PrepTime    = ParseIsoDuration(GetStr(recipeEl.Value, "prepTime") ?? ""),
                    CookTime    = ParseIsoDuration(GetStr(recipeEl.Value, "cookTime") ?? ""),
                    ImageUrl    = ParseImageUrl(recipeEl.Value),
                    Servings    = ParseServingsFromEl(recipeEl.Value),
                };

                if (recipeEl.Value.TryGetProperty("recipeIngredient", out var ings))
                    foreach (var ing in ings.EnumerateArray())
                        r.Ingredients.Add(ParseIngredientLine(ing.GetString() ?? ""));

                if (recipeEl.Value.TryGetProperty("recipeInstructions", out var steps))
                    r.Steps.AddRange(ParseStepsFromEl(steps));

                if (!string.IsNullOrEmpty(r.Title)) return r;
            }
            catch { }
        }
        return null;
    }

    private JsonElement? FindRecipeElement(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in el.EnumerateArray())
            {
                var f = FindRecipeElement(item);
                if (f != null) return f;
            }
        }
        else if (el.ValueKind == JsonValueKind.Object)
        {
            if (el.TryGetProperty("@type", out var t))
            {
                var s = t.ValueKind == JsonValueKind.Array
                    ? string.Join(",", t.EnumerateArray().Select(x => x.GetString()))
                    : t.GetString() ?? "";
                if (s.Contains("Recipe", StringComparison.OrdinalIgnoreCase)) return el;
            }
            if (el.TryGetProperty("@graph", out var graph))
                return FindRecipeElement(graph);
        }
        return null;
    }

    // ── 3. HTML-фолбэк ───────────────────────────────────────────────
    private ImportedRecipe? TryParseHtml(HtmlDocument doc)
    {
        var title = doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim();
        if (string.IsNullOrEmpty(title)) return null;

        var r = new ImportedRecipe { Title = CleanHtml(title) };

        // Описание — пробуем мета og:description
        var meta = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
        r.Description = CleanHtml(meta?.GetAttributeValue("content", "") ?? "");

        var ingNodes = doc.DocumentNode.SelectNodes("//*[@itemprop='recipeIngredient']");
        if (ingNodes != null)
            foreach (var n in ingNodes)
                r.Ingredients.Add(ParseIngredientLine(CleanHtml(n.InnerText)));

        var stepNodes = doc.DocumentNode.SelectNodes("//*[@itemprop='recipeInstructions']");
        if (stepNodes != null)
            foreach (var n in stepNodes)
            {
                var t = CleanHtml(n.InnerText);
                if (t.Length > 10) r.Steps.Add(t);
            }

        return r.Ingredients.Count > 0 || r.Steps.Count > 0 ? r : null;
    }

    // ── Вспомогательные ─────────────────────────────────────────────
    private int ParseServingsFromEl(JsonElement el)
    {
        if (!el.TryGetProperty("recipeYield", out var y) &&
            !el.TryGetProperty("servings", out y) &&
            !el.TryGetProperty("yield", out y)) return 0;

        if (y.ValueKind == JsonValueKind.Number) return y.GetInt32();
        var s = y.ValueKind == JsonValueKind.String ? y.GetString() ?? "" : y.ToString();
        var m = Regex.Match(s, @"\d+");
        return m.Success ? int.Parse(m.Value) : 0;
    }

    private string? ParseImageUrl(JsonElement el)
    {
        if (!el.TryGetProperty("image", out var img)) return null;
        if (img.ValueKind == JsonValueKind.String) return img.GetString();
        if (img.ValueKind == JsonValueKind.Array)
            return img.EnumerateArray().FirstOrDefault().GetString();
        if (img.ValueKind == JsonValueKind.Object)
            return img.TryGetProperty("url", out var u) ? u.GetString() : null;
        return null;
    }

    private ImportedIngredient ParseIngredientLine(string line)
    {
        line = line.Trim();
        var match = Regex.Match(line,
            @"^([\d½⅓⅔¼¾⅛⅜⅝⅞\s\.,/\-]+)\s*(г|кг|мл|л|ст\.л\.|ч\.л\.|шт|стакан[а-я]*|щепотк[а-я]*|пучок|зубчик[а-я]*|по вкусу)\.?\s+(.+)$",
            RegexOptions.IgnoreCase);

        if (match.Success)
            return new ImportedIngredient
            {
                Amount = match.Groups[1].Value.Trim(),
                Unit   = match.Groups[2].Value.Trim(),
                Name   = match.Groups[3].Value.Trim()
            };

        return new ImportedIngredient { Name = line };
    }

    private string? GetStr(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    }

    private double GetDouble(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return 0;
        if (v.ValueKind == JsonValueKind.Number) return v.GetDouble();
        if (double.TryParse(v.ToString(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        return 0;
    }

    private int ParseIsoDuration(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return 0;
        var h = Regex.Match(iso, @"(\d+)H");
        var m = Regex.Match(iso, @"(\d+)M");
        return (h.Success ? int.Parse(h.Groups[1].Value) * 60 : 0)
             + (m.Success ? int.Parse(m.Groups[1].Value) : 0);
    }

    private string CleanHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        text = Regex.Replace(text, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s{2,}", " ");
        return text.Trim();
    }

    private async Task<string?> DownloadImageAsync(string url)
    {
        try
        {
            var bytes = await _http.GetByteArrayAsync(url);
            var ext   = Path.GetExtension(new Uri(url).AbsolutePath).Split('?')[0];
            if (string.IsNullOrEmpty(ext)) ext = ".jpg";
            var dir  = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecipeApp", "Images");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, $"{Guid.NewGuid()}{ext}");
            await File.WriteAllBytesAsync(file, bytes);
            return file;
        }
        catch { return null; }
    }
}

using Microsoft.Extensions.FileProviders;
using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы
builder.Services.AddDirectoryBrowser(); // Для просмотра загруженных файлов


var app = builder.Build();


// Словарь для хранения ключей и названий компаний
//var keywordToCompanies = new Dictionary<string, List<string>>();
Dictionary<string, HashSet<string>> keywordToCompanies = new Dictionary<string, HashSet<string>>();

// Настройка маршрутов
app.UseStaticFiles(); // Для обслуживания статических файлов
app.UseRouting();


app.MapGet("/", async context =>
{
    string fullPath = $"{Directory.GetCurrentDirectory()}/index.html";
    await context.Response.WriteAsync(File.ReadAllText(fullPath));
});


// Обработка загрузки файла
app.MapPost("/upload", async context =>
{
    //response.ContentType = "text/html; charset=utf-8";
    var formFile = context.Request.Form.Files[0];
    keywordToCompanies.Clear();

    if (formFile.Length > 0)
    {
        var filePath = Path.Combine("uploads", formFile.FileName);

        // Создаем папку для загрузок, если ее нет
        if (!Directory.Exists("uploads"))
        {
            Directory.CreateDirectory("uploads");
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await formFile.CopyToAsync(stream);
        }

        #region dictoinary - read and write to the memory section
        // Читаем содержимое файла и сохраняем в словарь
        var content = await File.ReadAllTextAsync(filePath);
        //var lines = content.Split('\n');
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length != 2)
            {
                await context.Response.WriteAsync($"Ошибка в строке: '{line}'. Ожидалось 'название компании: ключи'.<br>");
                continue; // Пропускаем некорректные строки
            }
            var companyName = parts[0].Trim();
            var keys = parts[1].Split(',')
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrEmpty(k)) // Убираем пустые ключи
                            .ToList();

            // Проверка на наличие ключей
            if (keys.Count == 0)
            {
                await context.Response.WriteAsync($"Ошибка в строке: '{line}'. Ключи не могут быть пустыми.<br>");
                continue;
            }

            foreach (var key in keys)
            {
                // Проверка на корректность ключа
                //if (key.Contains(" ") || key.StartsWith("/") || key.EndsWith("/"))
                if (key.Contains(" ") || key.EndsWith("/"))
                {
                    await context.Response.WriteAsync($"Ошибка в строке: '{line}'. Ключ '{key}' некорректен.<br>");
                    continue;
                }

                if (!keywordToCompanies.ContainsKey(key))
                {
                    //keywordToCompanies[key] = new List<string>();
                    keywordToCompanies[key] = new HashSet<string>();
                }
                keywordToCompanies[key].Add(companyName);
            }
        }
        #endregion

        await context.Response.WriteAsync($"Файл '{formFile.FileName}' загружен успешно!", System.Text.Encoding.Default); // Возвращаем сообщение
    }
    else
    {
        await context.Response.WriteAsync("Ошибка: файл не загружен.");
    }
});


// Обработка поиска по ключу
app.MapGet("/search", async context =>
{
    var key = context.Request.Query["key"].ToString();
    // Проверка на корректность ключа
    if (key.Contains(" ") || key.EndsWith("/"))
    {
        await context.Response.WriteAsync($"Ошибка: ключ '{key}' некорректен. Ключ не должен содержать пробелы и не должен заканчиваться на '/'.<br>");
        return; // Завершаем выполнение, если ключ некорректен
    }

    var results = new HashSet<string>(); // Используем HashSet для уникальности

    // Разбиваем ключ на части
    var keyParts = key.Split('/').Where(part => !string.IsNullOrEmpty(part)).ToList();

    // Проходим по всем частям ключа
    for (int i = 0; i < keyParts.Count; i++)
    {
        var currentKey = "/" + string.Join("/", keyParts.Take(i + 1)); // Собираем текущий ключ

        if (keywordToCompanies.ContainsKey(currentKey))
        {
            foreach (var company in keywordToCompanies[currentKey])
            {
                results.Add(company);
            }
        }
    }

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync("<h1>Результаты поиска:</h1><ul>");
    foreach (var company in results)
    {
        await context.Response.WriteAsync($"<li>{company}</li>");
    }
    await context.Response.WriteAsync("</ul>");
});


// Отображение загруженных файлов
app.MapGet("/uploads", async context =>
{
    var files = Directory.GetFiles("uploads");
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync("<h1>Загруженные файлы:</h1><ul>");
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);
        await context.Response.WriteAsync($"<li><a href='/uploads/{fileName}'>{fileName}</a></li>");
    }
    await context.Response.WriteAsync("</ul>");
});


app.Run();



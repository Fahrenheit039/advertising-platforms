using Microsoft.Extensions.FileProviders;
using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// ��������� �������
builder.Services.AddDirectoryBrowser(); // ��� ��������� ����������� ������


var app = builder.Build();


// ������� ��� �������� ������ � �������� ��������
//var keywordToCompanies = new Dictionary<string, List<string>>();
Dictionary<string, HashSet<string>> keywordToCompanies = new Dictionary<string, HashSet<string>>();

// ��������� ���������
app.UseStaticFiles(); // ��� ������������ ����������� ������
app.UseRouting();


app.MapGet("/", async context =>
{
    string fullPath = $"{Directory.GetCurrentDirectory()}/index.html";
    await context.Response.WriteAsync(File.ReadAllText(fullPath));
});


// ��������� �������� �����
app.MapPost("/upload", async context =>
{
    //response.ContentType = "text/html; charset=utf-8";
    var formFile = context.Request.Form.Files[0];
    keywordToCompanies.Clear();

    if (formFile.Length > 0)
    {
        var filePath = Path.Combine("uploads", formFile.FileName);

        // ������� ����� ��� ��������, ���� �� ���
        if (!Directory.Exists("uploads"))
        {
            Directory.CreateDirectory("uploads");
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await formFile.CopyToAsync(stream);
        }

        #region dictoinary - read and write to the memory section
        // ������ ���������� ����� � ��������� � �������
        var content = await File.ReadAllTextAsync(filePath);
        //var lines = content.Split('\n');
        var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            if (parts.Length != 2)
            {
                await context.Response.WriteAsync($"������ � ������: '{line}'. ��������� '�������� ��������: �����'.<br>");
                continue; // ���������� ������������ ������
            }
            var companyName = parts[0].Trim();
            var keys = parts[1].Split(',')
                            .Select(k => k.Trim())
                            .Where(k => !string.IsNullOrEmpty(k)) // ������� ������ �����
                            .ToList();

            // �������� �� ������� ������
            if (keys.Count == 0)
            {
                await context.Response.WriteAsync($"������ � ������: '{line}'. ����� �� ����� ���� �������.<br>");
                continue;
            }

            foreach (var key in keys)
            {
                // �������� �� ������������ �����
                //if (key.Contains(" ") || key.StartsWith("/") || key.EndsWith("/"))
                if (key.Contains(" ") || key.EndsWith("/"))
                {
                    await context.Response.WriteAsync($"������ � ������: '{line}'. ���� '{key}' �����������.<br>");
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

        await context.Response.WriteAsync($"���� '{formFile.FileName}' �������� �������!", System.Text.Encoding.Default); // ���������� ���������
    }
    else
    {
        await context.Response.WriteAsync("������: ���� �� ��������.");
    }
});


// ��������� ������ �� �����
app.MapGet("/search", async context =>
{
    var key = context.Request.Query["key"].ToString();
    // �������� �� ������������ �����
    if (key.Contains(" ") || key.EndsWith("/"))
    {
        await context.Response.WriteAsync($"������: ���� '{key}' �����������. ���� �� ������ ��������� ������� � �� ������ ������������� �� '/'.<br>");
        return; // ��������� ����������, ���� ���� �����������
    }

    var results = new HashSet<string>(); // ���������� HashSet ��� ������������

    // ��������� ���� �� �����
    var keyParts = key.Split('/').Where(part => !string.IsNullOrEmpty(part)).ToList();

    // �������� �� ���� ������ �����
    for (int i = 0; i < keyParts.Count; i++)
    {
        var currentKey = "/" + string.Join("/", keyParts.Take(i + 1)); // �������� ������� ����

        if (keywordToCompanies.ContainsKey(currentKey))
        {
            foreach (var company in keywordToCompanies[currentKey])
            {
                results.Add(company);
            }
        }
    }

    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync("<h1>���������� ������:</h1><ul>");
    foreach (var company in results)
    {
        await context.Response.WriteAsync($"<li>{company}</li>");
    }
    await context.Response.WriteAsync("</ul>");
});


// ����������� ����������� ������
app.MapGet("/uploads", async context =>
{
    var files = Directory.GetFiles("uploads");
    context.Response.ContentType = "text/html";
    await context.Response.WriteAsync("<h1>����������� �����:</h1><ul>");
    foreach (var file in files)
    {
        var fileName = Path.GetFileName(file);
        await context.Response.WriteAsync($"<li><a href='/uploads/{fileName}'>{fileName}</a></li>");
    }
    await context.Response.WriteAsync("</ul>");
});


app.Run();



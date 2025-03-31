
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace advertising_platforms.Tests;

public class UploadTests
{
    private Dictionary<string, HashSet<string>> keywordToCompanies;

    public UploadTests()
    {
        keywordToCompanies = new Dictionary<string, HashSet<string>>();
    }

    [Fact]
    public async Task TestUploadFile_ClearsPreviousData()
    {
        // Arrange
        keywordToCompanies["/keyword1"] = new HashSet<string> { "CompanyA" };
        var fileContent = "/keyword2:CompanyB\n/keyword3:CompanyC";
        var file = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        // Act
        await UploadFile(file);

        // Assert
        //Assert.Empty(keywordToCompanies["/keyword1"]); // Должно быть очищено
        Assert.False(keywordToCompanies.ContainsKey("/keyword1")); // Должно быть очищено
        Assert.Contains("CompanyB", keywordToCompanies["/keyword2"]);
        Assert.Contains("CompanyC", keywordToCompanies["/keyword3"]);
    }

    private async Task UploadFile(Stream fileStream)
    {
        keywordToCompanies.Clear();
        // Логика загрузки файла
        using (var reader = new StreamReader(fileStream))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var company = parts[1].Trim();

                    if (!keywordToCompanies.ContainsKey(key))
                    {
                        keywordToCompanies[key] = new HashSet<string>();
                    }
                    keywordToCompanies[key].Add(company);
                }
            }
        }
    }
}

public class SearchTests
{
    private Dictionary<string, HashSet<string>> keywordToCompanies;

    public SearchTests()
    {
        keywordToCompanies = new Dictionary<string, HashSet<string>>();
        // Заполняем словарь для тестов
        keywordToCompanies["/keyword1"] = new HashSet<string> { "CompanyA" };
    }

    [Theory]
    [InlineData("/keyword1")]
    [InlineData("/keyword2")] // Не существует, но тестируем
    public void TestSearch_ReturnsCorrectResults(string key)
    {
        // Act
        var results = Search(key);

        // Assert
        if (key == "/keyword1")
        {
            Assert.Contains("CompanyA", results);
        }
        else
        {
            Assert.Empty(results);
        }
    }

    private HashSet<string> Search(string key)
    {
        return keywordToCompanies.ContainsKey(key) ? new HashSet<string>(keywordToCompanies[key]) : new HashSet<string>();
    }
}

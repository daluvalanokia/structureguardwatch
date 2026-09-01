using Microsoft.AspNetCore.Mvc;
using StructureWatch.Web.Controllers;
using StructureWatch.Core.Services;
using Moq;

namespace StructureWatch.Web.Tests;

public class SearchControllerTests
{
    [Fact]
    public async Task Search_ReturnsBadRequest_ForShortQuery()
    {
        var nominatim = new Mock<INominatimService>();
        var controller = new SearchController(nominatim.Object);

        var result = await controller.Search("ab");

        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var list = (IEnumerable<object>)okResult.Value!;
        Assert.Empty(list);
    }

    [Fact]
    public async Task Search_ReturnsResults_ForValidQuery()
    {
        var nominatim = new Mock<INominatimService>();
        var mockResults = new List<SearchResult>
        {
            new("New York, NY, USA", 40.7128, -74.0060, "New York", "New York", "USA"),
            new("New York, Lincolnshire, UK", 53.1072, -0.8010, "New York", "Lincolnshire", "UK"),
        };
        nominatim.Setup(s => s.SearchAsync("New York", 5)).ReturnsAsync(mockResults);

        var controller = new SearchController(nominatim.Object);
        var result = await controller.Search("New York");

        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var list = (IEnumerable<dynamic>)okResult.Value!;
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task Search_ReturnsEmpty_ForWhitespaceQuery()
    {
        var nominatim = new Mock<INominatimService>();
        var controller = new SearchController(nominatim.Object);

        var result = await controller.Search("   ");

        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        var list = (IEnumerable<object>)okResult.Value!;
        Assert.Empty(list);
    }
}

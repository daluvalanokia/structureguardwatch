using Microsoft.AspNetCore.Mvc;
using StructureWatch.Web.Controllers;
using StructureWatch.Core.Services;
using Moq;

namespace StructureWatch.Web.Tests;

public class AnalysisControllerTests
{
    [Fact]
    public async Task Analyze_ReturnsBadRequest_WhenOsmIdMissing()
    {
        var agent = new Mock<ITokenSaverAgent>();
        var db = new Mock<StructureWatch.Data.StructureWatchDbContext>();
        var controller = new AnalysisController(agent.Object, db.Object);

        var result = await controller.Analyze(new AnalyzeRequest { OsmId = "", Tags = new() });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Analyze_ReturnsBadRequest_WhenTagsNull()
    {
        var agent = new Mock<ITokenSaverAgent>();
        var db = new Mock<StructureWatch.Data.StructureWatchDbContext>();
        var controller = new AnalysisController(agent.Object, db.Object);

        var result = await controller.Analyze(new AnalyzeRequest { OsmId = "way/123", Tags = null! });

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
